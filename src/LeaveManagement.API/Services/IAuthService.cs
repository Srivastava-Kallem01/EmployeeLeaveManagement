using LeaveManagement.API.DTOs;

namespace LeaveManagement.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto);
}
