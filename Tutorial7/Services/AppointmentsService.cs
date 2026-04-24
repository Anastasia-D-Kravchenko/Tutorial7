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
}