using LeaveManagement.API.DTOs;

namespace LeaveManagement.API.Services;

public interface ILeaveService
{
    Task<LeaveRequestResponseDto> CreateAsync(int employeeId, CreateLeaveRequestDto dto);
    Task<IEnumerable<LeaveRequestResponseDto>> GetByEmployeeAsync(int employeeId);
    Task<IEnumerable<LeaveRequestResponseDto>> GetAllPendingAsync();
    Task<LeaveRequestResponseDto?> GetByIdAsync(int requestId);
    Task<bool> ReviewAsync(int requestId, ReviewLeaveRequestDto dto);
}
