namespace Tutorial7.DTOs;

public class AppointmentDetailsDto
{
    public int      IdAppointment    { get; set; }
    public DateTime AppointmentDate  { get; set; }
    public string   Status           { get; set; } = string.Empty;
    public string   Reason           { get; set; } = string.Empty;
    public string?  InternalNotes    { get; set; }
    public DateTime CreatedAt        { get; set; }
    // Patient
    public string   PatientFullName  { get; set; } = string.Empty;
    public string   PatientEmail     { get; set; } = string.Empty;
    public string   PatientPhone     { get; set; } = string.Empty;
    // Doctor + Specialization
    public string   DoctorFullName   { get; set; } = string.Empty;
    public string   LicenseNumber    { get; set; } = string.Empty;
    public string   Specialization   { get; set; } = string.Empty;
}