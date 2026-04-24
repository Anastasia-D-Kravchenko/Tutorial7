using Microsoft.AspNetCore.Mvc;
using Tutorial7.DTOs;
using Tutorial7.Services;

namespace ClinicAdoNet.Controllers;

[Route("api/[controller]")] 
[ApiController]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsService _service;

    public AppointmentsController(IAppointmentsService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AppointmentListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? patientLastName)
    {
        var appointments = await _service.GetAllAppointmentsAsync(status, patientLastName);
        return Ok(appointments);
    }

    [HttpGet("{idAppointment:int}")]
    [ProducesResponseType(typeof(AppointmentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto),      StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int idAppointment)
    {
        var dto = await _service.GetAppointmentByIdAsync(idAppointment);
        if (dto is null)
            return NotFound(new ErrorResponseDto
                { Message = $"Appointment {idAppointment} not found." });

        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequestDto dto)
    {
        try
        {
            var newId = await _service.CreateAppointmentAsync(dto);

            return CreatedAtAction(nameof(GetById),
                new { idAppointment = newId },
                new { idAppointment = newId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto { Message = ex.Message });
        }
    }

    [HttpPut("{idAppointment:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int idAppointment,
        [FromBody] UpdateAppointmentRequestDto dto)
    {
        try
        {
            var found = await _service.UpdateAppointmentAsync(idAppointment, dto);
            if (!found)
                return NotFound(new ErrorResponseDto
                    { Message = $"Appointment {idAppointment} not found." });

            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto { Message = ex.Message });
        }
    }

    [HttpDelete("{idAppointment:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int idAppointment)
    {
        try
        {
            var found = await _service.DeleteAppointmentAsync(idAppointment);
            if (!found)
                return NotFound(new ErrorResponseDto
                    { Message = $"Appointment {idAppointment} not found." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto { Message = ex.Message });
        }
    }
}
