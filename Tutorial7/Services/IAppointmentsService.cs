using Tutorial7.DTOs;

namespace Tutorial7.Services;

public interface IAppointmentsService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(
        string? status, string? patientLastName);

    Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int idAppointment);

    Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto dto);

    Task<bool> UpdateAppointmentAsync(int idAppointment, UpdateAppointmentRequestDto dto);

    Task<bool> DeleteAppointmentAsync(int idAppointment);
}