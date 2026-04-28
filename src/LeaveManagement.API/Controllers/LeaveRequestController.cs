using System.Security.Claims;
using LeaveManagement.API.DTOs;
using LeaveManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.API.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
[Produces("application/json")]
public class LeaveRequestController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeaveRequestController(ILeaveService leaveService) => _leaveService = leaveService;

    private int CurrentEmployeeId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Submit a new leave request (authenticated employee).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LeaveRequestResponseDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
    {
        var result = await _leaveService.CreateAsync(CurrentEmployeeId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Get the authenticated employee's own leave requests.</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<LeaveRequestResponseDto>), 200)]
    public async Task<IActionResult> GetMine()
    {
        var result = await _leaveService.GetByEmployeeAsync(CurrentEmployeeId);
        return Ok(result);
    }

    /// <summary>Get a single leave request by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LeaveRequestResponseDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _leaveService.GetByIdAsync(id);
        if (result is null) return NotFound(new { message = "Leave request not found." });
        return Ok(result);
    }

    /// <summary>Get all pending leave requests (Manager/Admin only).</summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(IEnumerable<LeaveRequestResponseDto>), 200)]
    public async Task<IActionResult> GetPending()
    {
        var result = await _leaveService.GetAllPendingAsync();
        return Ok(result);
    }

    /// <summary>Approve or reject a leave request (Manager/Admin only).</summary>
    [HttpPut("{id:int}/review")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewLeaveRequestDto dto)
    {
        if (dto.Status != "Approved" && dto.Status != "Rejected")
            return BadRequest(new { message = "Status must be 'Approved' or 'Rejected'." });

        var success = await _leaveService.ReviewAsync(id, dto);
        if (!success) return NotFound(new { message = "Leave request not found." });

        return Ok(new { message = $"Leave request {dto.Status.ToLower()} successfully." });
    }
}
