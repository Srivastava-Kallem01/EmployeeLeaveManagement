using LeaveManagement.API.Data;
using LeaveManagement.API.DTOs;
using LeaveManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.API.Services;

public class LeaveService : ILeaveService
{
    private readonly AppDbContext _db;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(AppDbContext db, ILogger<LeaveService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<LeaveRequestResponseDto> CreateAsync(int employeeId, CreateLeaveRequestDto dto)
    {
        if (dto.EndDate < dto.StartDate)
            throw new InvalidOperationException("End date cannot be before start date.");

        if (dto.StartDate.Date < DateTime.UtcNow.Date)
            throw new InvalidOperationException("Leave cannot be requested for past dates.");

        var employee = await _db.Employees.FindAsync(employeeId)
            ?? throw new InvalidOperationException("Employee not found.");

        var days = (dto.EndDate.Date - dto.StartDate.Date).Days + 1;

        if (dto.LeaveType == "Annual" && employee.AnnualLeaveBalance < days)
            throw new InvalidOperationException(
                $"Insufficient leave balance. Requested: {days} days, Available: {employee.AnnualLeaveBalance} days.");

        var overlapping = await _db.LeaveRequests.AnyAsync(r =>
            r.EmployeeId == employeeId &&
            r.Status != "Rejected" &&
            r.StartDate <= dto.EndDate &&
            r.EndDate >= dto.StartDate);

        if (overlapping)
            throw new InvalidOperationException("You already have a leave request for these dates.");

        var request = new LeaveRequest
        {
            EmployeeId = employeeId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            LeaveType = dto.LeaveType,
            Reason = dto.Reason,
            Status = "Pending"
        };

        _db.LeaveRequests.Add(request);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Leave request {Id} created for employee {EmployeeId} ({Days} days)", request.Id, employeeId, days);

        return MapToResponse(request, employee.FullName);
    }

    public async Task<IEnumerable<LeaveRequestResponseDto>> GetByEmployeeAsync(int employeeId)
    {
        var requests = await _db.LeaveRequests
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync();

        return requests.Select(x => MapToResponse(x, x.Employee.FullName));
    }

    public async Task<IEnumerable<LeaveRequestResponseDto>> GetAllPendingAsync()
    {
        var requests = await _db.LeaveRequests
            .Include(x => x.Employee)
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.StartDate)
            .ToListAsync();

        return requests.Select(x => MapToResponse(x, x.Employee.FullName));
    }

    public async Task<LeaveRequestResponseDto?> GetByIdAsync(int requestId)
    {
        var request = await _db.LeaveRequests
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == requestId);

        return request is null ? null : MapToResponse(request, request.Employee.FullName);
    }

    public async Task<bool> ReviewAsync(int requestId, ReviewLeaveRequestDto dto)
    {
        var request = await _db.LeaveRequests
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == requestId);

        if (request is null) return false;
        if (request.Status != "Pending")
            throw new InvalidOperationException("Only pending requests can be reviewed.");

        request.Status = dto.Status;
        request.ManagerComment = dto.ManagerComment;
        request.ReviewedAt = DateTime.UtcNow;

        if (dto.Status == "Approved" && request.LeaveType == "Annual")
        {
            var days = (request.EndDate.Date - request.StartDate.Date).Days + 1;
            request.Employee.AnnualLeaveBalance -= days;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Leave request {Id} reviewed as {Status}", requestId, dto.Status);
        return true;
    }

    private static LeaveRequestResponseDto MapToResponse(LeaveRequest r, string employeeName)
    {
        var days = (r.EndDate.Date - r.StartDate.Date).Days + 1;
        return new LeaveRequestResponseDto(
            r.Id, employeeName, r.StartDate, r.EndDate,
            r.LeaveType, r.Reason, r.Status, r.ManagerComment, r.RequestedAt, days
        );
    }
}
