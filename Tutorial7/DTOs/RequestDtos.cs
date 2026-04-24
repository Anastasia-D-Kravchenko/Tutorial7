using System.ComponentModel.DataAnnotations;

namespace Tutorial7.DTOs;

public class CreateAppointmentRequestDto
{
    [Required] public int      IdPatient       { get; set; }
    [Required] public int      IdDoctor        { get; set; }
    [Required] public DateTime AppointmentDate { get; set; }
    [Required][MaxLength(250)]
    public string   Reason          { get; set; } = string.Empty;
}

public class UpdateAppointmentRequestDto
{
    [Required] public int      IdPatient       { get; set; }
    [Required] public int      IdDoctor        { get; set; }
    [Required] public DateTime AppointmentDate { get; set; }
    [Required][RegularExpression("^(Scheduled|Completed|Cancelled)$")]
    public string   Status          { get; set; } = string.Empty;
    [Required][MaxLength(250)]
    public string   Reason          { get; set; } = string.Empty;
    [MaxLength(500)]
    public string?  InternalNotes   { get; set; }
}

public class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
}