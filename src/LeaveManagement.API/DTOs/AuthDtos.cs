using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.API.DTOs;

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password
);

public record RegisterDto(
    [Required] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string Department,
    string Role = "Employee"
);

public record AuthResponseDto(
    string Token,
    string FullName,
    string Role,
    DateTime ExpiresAt
);
