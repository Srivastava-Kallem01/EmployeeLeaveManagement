using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.API.DTOs;

public record CreateLeaveRequestDto(
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    [Required] string LeaveType,   // Annual | Sick | Unpaid
    [Required, MaxLength(500)] string Reason
);

public record ReviewLeaveRequestDto(
    [Required] string Status,      // Approved | Rejected
    [MaxLength(500)] string? ManagerComment
);

public record LeaveRequestResponseDto(
    int Id,
    string EmployeeName,
    DateTime StartDate,
    DateTime EndDate,
    string LeaveType,
    string Reason,
    string Status,
    string? ManagerComment,
    DateTime RequestedAt,
    int DaysRequested
);
