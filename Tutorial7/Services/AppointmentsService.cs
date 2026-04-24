using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial7.DTOs;

namespace Tutorial7.Services;

public class AppointmentsService : IAppointmentsService
{
    private readonly string _connectionString;

    public AppointmentsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(
        string? status, string? patientLastName)
    {
        const string query = """
            SELECT  a.IdAppointment,
                    a.AppointmentDate,
                    a.Status,
                    a.Reason,
                    p.FirstName + N' ' + p.LastName AS PatientFullName,
                    p.Email                          AS PatientEmail
            FROM    dbo.Appointments a
            JOIN    dbo.Patients     p ON p.IdPatient = a.IdPatient
            WHERE  (@Status         IS NULL OR a.Status    = @Status)
              AND  (@PatientLastName IS NULL OR p.LastName  = @PatientLastName)
            ORDER BY a.AppointmentDate;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value =
            (object?)status ?? DBNull.Value;
        command.Parameters.Add("@PatientLastName", SqlDbType.NVarChar, 80).Value =
            (object?)patientLastName ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync();

        var appointments = new List<AppointmentListDto>();
        while (await reader.ReadAsync())
        {
            var appointment = new AppointmentListDto
            {
                IdAppointment   = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status          = reader.GetString(2),
                Reason          = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail    = reader.GetString(5),
            };
            appointments.Add(appointment);
        }

        return appointments;
    }

    public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int idAppointment)
    {
        const string query = """
            SELECT  a.IdAppointment,
                    a.AppointmentDate,
                    a.Status,
                    a.Reason,
                    a.InternalNotes,
                    a.CreatedAt,
                    p.FirstName + N' ' + p.LastName AS PatientFullName,
                    p.Email                          AS PatientEmail,
                    p.PhoneNumber                    AS PatientPhone,
                    d.FirstName + N' ' + d.LastName  AS DoctorFullName,
                    d.LicenseNumber,
                    s.Name                           AS Specialization
            FROM    dbo.Appointments    a
            JOIN    dbo.Patients        p ON p.IdPatient        = a.IdPatient
            JOIN    dbo.Doctors         d ON d.IdDoctor         = a.IdDoctor
            JOIN    dbo.Specializations s ON s.IdSpecialization = d.IdSpecialization
            WHERE   a.IdAppointment = @IdAppointment;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new AppointmentDetailsDto
        {
            IdAppointment   = reader.GetInt32(0),
            AppointmentDate = reader.GetDateTime(1),
            Status          = reader.GetString(2),
            Reason          = reader.GetString(3),
            InternalNotes   = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt       = reader.GetDateTime(5),
            PatientFullName = reader.GetString(6),
            PatientEmail    = reader.GetString(7),
            PatientPhone    = reader.GetString(8),
            DoctorFullName  = reader.GetString(9),
            LicenseNumber   = reader.GetString(10),
            Specialization  = reader.GetString(11),
        };
    }

    public async Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto dto)
    {
        if (dto.AppointmentDate <= DateTime.UtcNow)
            throw new ArgumentException("Appointment date must be in the future.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new ArgumentException("Reason cannot be empty.");

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await ValidatePatientAsync(connection, dto.IdPatient);
        await ValidateDoctorAsync(connection, dto.IdDoctor);
        await CheckDoctorConflictAsync(connection, dto.IdDoctor, dto.AppointmentDate, null);

        const string sql = """
            INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason)
            OUTPUT INSERTED.IdAppointment
            VALUES (@IdPatient, @IdDoctor, @AppointmentDate, N'Scheduled', @Reason);
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@IdPatient",       SqlDbType.Int).Value           = dto.IdPatient;
        command.Parameters.Add("@IdDoctor",        SqlDbType.Int).Value           = dto.IdDoctor;
        command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value     = dto.AppointmentDate;
        command.Parameters.Add("@Reason",          SqlDbType.NVarChar, 250).Value = dto.Reason;

        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task ValidatePatientAsync(SqlConnection connection, int idPatient)
    {
        const string sql = "SELECT IsActive FROM dbo.Patients WHERE IdPatient = @IdPatient;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@IdPatient", SqlDbType.Int).Value = idPatient;
        var result = await cmd.ExecuteScalarAsync();
        if (result is null)  throw new ArgumentException($"Patient {idPatient} not found.");
        if (!(bool)result)   throw new ArgumentException($"Patient {idPatient} is not active.");
    }

    private static async Task ValidateDoctorAsync(SqlConnection connection, int idDoctor)
    {
        const string sql = "SELECT IsActive FROM dbo.Doctors WHERE IdDoctor = @IdDoctor;";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
        var result = await cmd.ExecuteScalarAsync();
        if (result is null)  throw new ArgumentException($"Doctor {idDoctor} not found.");
        if (!(bool)result)   throw new ArgumentException($"Doctor {idDoctor} is not active.");
    }

    private static async Task CheckDoctorConflictAsync(
        SqlConnection connection, int idDoctor, DateTime appointmentDate, int? excludeId)
    {
        const string sql = """
            SELECT COUNT(1) FROM dbo.Appointments
            WHERE  IdDoctor        = @IdDoctor
              AND  AppointmentDate = @AppointmentDate
              AND  Status          = N'Scheduled'
              AND  (@ExcludeId     IS NULL OR IdAppointment <> @ExcludeId);
            """;
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@IdDoctor",        SqlDbType.Int).Value       = idDoctor;
        cmd.Parameters.Add("@AppointmentDate", SqlDbType.DateTime2).Value = appointmentDate;
        cmd.Parameters.Add("@ExcludeId",       SqlDbType.Int).Value       =
            (object?)excludeId ?? DBNull.Value;
        var count = (int)(await cmd.ExecuteScalarAsync())!;
        if (count > 0)
            throw new InvalidOperationException("The doctor already has a Scheduled appointment at this time.");
    }
}