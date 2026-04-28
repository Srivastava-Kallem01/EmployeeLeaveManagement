using LeaveManagement.API.Data;
using LeaveManagement.API.DTOs;
using LeaveManagement.API.Models;
using LeaveManagement.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LeaveManagement.Tests;

public class LeaveServiceTests
{
    private static AppDbContext CreateDbContext(string dbName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    private static LeaveService CreateService(AppDbContext db) =>
        new(db, new Mock<ILogger<LeaveService>>().Object);

    private static Employee SeedEmployee(AppDbContext db, int balance = 20)
    {
        var employee = new Employee
        {
            FullName = "Test User",
            Email = $"test{Guid.NewGuid():N}@example.com",
            PasswordHash = "hashed",
            Department = "Engineering",
            Role = "Employee",
            AnnualLeaveBalance = balance
        };
        db.Employees.Add(employee);
        db.SaveChanges();
        return employee;
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsRequestWithPendingStatus()
    {
        var db = CreateDbContext(nameof(CreateAsync_ValidRequest_ReturnsRequestWithPendingStatus));
        var employee = SeedEmployee(db, balance: 20);
        var service = CreateService(db);

        var dto = new CreateLeaveRequestDto(
            DateTime.UtcNow.Date.AddDays(2),
            DateTime.UtcNow.Date.AddDays(4),
            "Annual",
            "Family vacation"
        );

        var result = await service.CreateAsync(employee.Id, dto);

        Assert.Equal("Pending", result.Status);
        Assert.Equal("Test User", result.EmployeeName);
        Assert.Equal(3, result.DaysRequested);
    }

    [Fact]
    public async Task CreateAsync_InsufficientBalance_ThrowsInvalidOperationException()
    {
        var db = CreateDbContext(nameof(CreateAsync_InsufficientBalance_ThrowsInvalidOperationException));
        var employee = SeedEmployee(db, balance: 2);
        var service = CreateService(db);

        var dto = new CreateLeaveRequestDto(
            DateTime.UtcNow.Date.AddDays(1),
            DateTime.UtcNow.Date.AddDays(10),
            "Annual",
            "Long vacation"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(employee.Id, dto));
    }

    [Fact]
    public async Task CreateAsync_PastStartDate_ThrowsInvalidOperationException()
    {
        var db = CreateDbContext(nameof(CreateAsync_PastStartDate_ThrowsInvalidOperationException));
        var employee = SeedEmployee(db);
        var service = CreateService(db);

        var dto = new CreateLeaveRequestDto(
            DateTime.UtcNow.Date.AddDays(-3),
            DateTime.UtcNow.Date.AddDays(-1),
            "Annual",
            "Past leave"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(employee.Id, dto));
    }

    [Fact]
    public async Task CreateAsync_OverlappingDates_ThrowsInvalidOperationException()
    {
        var db = CreateDbContext(nameof(CreateAsync_OverlappingDates_ThrowsInvalidOperationException));
        var employee = SeedEmployee(db, balance: 20);
        var service = CreateService(db);

        var dto = new CreateLeaveRequestDto(
            DateTime.UtcNow.Date.AddDays(3),
            DateTime.UtcNow.Date.AddDays(7),
            "Annual",
            "First request"
        );
        await service.CreateAsync(employee.Id, dto);

        var overlapping = new CreateLeaveRequestDto(
            DateTime.UtcNow.Date.AddDays(5),
            DateTime.UtcNow.Date.AddDays(9),
            "Annual",
            "Overlapping request"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(employee.Id, overlapping));
    }

    [Fact]
    public async Task ReviewAsync_Approved_DeductsAnnualLeaveBalance()
    {
        var db = CreateDbContext(nameof(ReviewAsync_Approved_DeductsAnnualLeaveBalance));
        var employee = SeedEmployee(db, balance: 15);
        var service = CreateService(db);

        var createDto = new CreateLeaveRequestDto(
            DateTime.UtcNow.Date.AddDays(1),
            DateTime.UtcNow.Date.AddDays(5),
            "Annual",
            "Rest"
        );
        var request = await service.CreateAsync(employee.Id, createDto);

        var reviewDto = new ReviewLeaveRequestDto("Approved", "Enjoy your time off!");
        var success = await service.ReviewAsync(request.Id, reviewDto);

        var updatedEmployee = await db.Employees.FindAsync(employee.Id);

        Assert.True(success);
        Assert.Equal(10, updatedEmployee!.AnnualLeaveBalance); // 15 - 5 days
    }

    [Fact]
    public async Task ReviewAsync_Rejected_DoesNotChangeLeaveBalance()
    {
        var db = CreateDbContext(nameof(ReviewAsync_Rejected_DoesNotChangeLeaveBalance));
        var employee = SeedEmployee(db, balance: 15);
        var service = CreateService(db);

        var createDto = new CreateLeaveRequestDto(
            DateTime.UtcNow.Date.AddDays(1),
            DateTime.UtcNow.Date.AddDays(3),
            "Annual",
            "Trip"
        );
        var request = await service.CreateAsync(employee.Id, createDto);

        var reviewDto = new ReviewLeaveRequestDto("Rejected", "Insufficient coverage.");
        await service.ReviewAsync(request.Id, reviewDto);

        var updatedEmployee = await db.Employees.FindAsync(employee.Id);
        Assert.Equal(15, updatedEmployee!.AnnualLeaveBalance);
    }

    [Fact]
    public async Task ReviewAsync_NonExistentRequest_ReturnsFalse()
    {
        var db = CreateDbContext(nameof(ReviewAsync_NonExistentRequest_ReturnsFalse));
        var service = CreateService(db);

        var result = await service.ReviewAsync(999, new ReviewLeaveRequestDto("Approved", null));

        Assert.False(result);
    }

    [Fact]
    public async Task GetByEmployeeAsync_ReturnsOnlyThatEmployeesRequests()
    {
        var db = CreateDbContext(nameof(GetByEmployeeAsync_ReturnsOnlyThatEmployeesRequests));
        var emp1 = SeedEmployee(db, balance: 20);
        var emp2 = SeedEmployee(db, balance: 20);
        var service = CreateService(db);

        await service.CreateAsync(emp1.Id, new CreateLeaveRequestDto(DateTime.UtcNow.Date.AddDays(2), DateTime.UtcNow.Date.AddDays(3), "Annual", "Emp1 leave"));
        await service.CreateAsync(emp2.Id, new CreateLeaveRequestDto(DateTime.UtcNow.Date.AddDays(4), DateTime.UtcNow.Date.AddDays(5), "Sick", "Emp2 leave"));

        var emp1Requests = (await service.GetByEmployeeAsync(emp1.Id)).ToList();

        Assert.Single(emp1Requests);
        Assert.All(emp1Requests, r => Assert.Equal("Test User", r.EmployeeName));
    }
}
