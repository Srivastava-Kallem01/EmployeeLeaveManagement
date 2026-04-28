using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeaveManagement.API.Data;
using LeaveManagement.API.DTOs;
using LeaveManagement.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LeaveManagement.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == dto.Email);
        if (employee is null || !BCrypt.Net.BCrypt.Verify(dto.Password, employee.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for {Email}", dto.Email);
            return null;
        }

        _logger.LogInformation("Successful login for {Email}", dto.Email);
        return GenerateToken(employee);
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto)
    {
        if (await _db.Employees.AnyAsync(e => e.Email == dto.Email))
            return (false, "An account with this email already exists.");

        var allowedRoles = new[] { "Admin", "Manager", "Employee" };
        if (!allowedRoles.Contains(dto.Role))
            return (false, $"Role must be one of: {string.Join(", ", allowedRoles)}.");

        var employee = new Employee
        {
            FullName = dto.FullName,
            Email = dto.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Department = dto.Department,
            Role = dto.Role
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New employee registered: {Email} as {Role}", dto.Email, dto.Role);
        return (true, "Employee registered successfully.");
    }

    private AuthResponseDto GenerateToken(Employee employee)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var expires = DateTime.UtcNow.AddHours(8);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new Claim(ClaimTypes.Email, employee.Email),
            new Claim(ClaimTypes.Name, employee.FullName),
            new Claim(ClaimTypes.Role, employee.Role),
            new Claim("department", employee.Department)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new AuthResponseDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            employee.FullName,
            employee.Role,
            expires
        );
    }
}
