# Employee Leave Management System

A production-grade REST API for managing employee leave requests with role-based access control, JWT authentication, and structured logging.

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 7, ASP.NET Core Web API |
| ORM | Entity Framework Core 7 |
| Database | SQL Server |
| Auth | JWT Bearer + RBAC |
| Logging | Serilog (Console + File) |
| API Docs | Swagger / OpenAPI |
| Testing | xUnit + Moq + EF InMemory |

## Features

- **JWT Authentication** — login returns a signed token; all endpoints require it
- **Three-role RBAC** — `Employee`, `Manager`, `Admin` with route-level `[Authorize(Roles)]`
- **Leave lifecycle** — submit → pending → approved/rejected with balance deduction
- **Business validations** — overlap detection, past-date rejection, balance checks
- **Global exception middleware** — consistent JSON error responses across the API
- **EF Core migrations** — applied automatically on startup
- **Serilog** — structured logs to console and rolling daily files under `logs/`

## Roles & Permissions

| Endpoint | Employee | Manager | Admin |
|---|:---:|:---:|:---:|
| `POST /api/auth/register` | ✓ | ✓ | ✓ |
| `POST /api/auth/login` | ✓ | ✓ | ✓ |
| `POST /api/leave-requests` | ✓ | ✓ | ✓ |
| `GET /api/leave-requests/my` | ✓ | ✓ | ✓ |
| `GET /api/leave-requests/{id}` | ✓ | ✓ | ✓ |
| `GET /api/leave-requests/pending` | ✗ | ✓ | ✓ |
| `PUT /api/leave-requests/{id}/review` | ✗ | ✓ | ✓ |

## Project Structure

```
EmployeeLeaveManagement/
├── src/
│   ├── LeaveManagement.API/
│   │   ├── Controllers/       # Auth + LeaveRequest endpoints
│   │   ├── Data/              # EF Core DbContext
│   │   ├── DTOs/              # Request/response contracts
│   │   ├── Middleware/        # Global exception handler
│   │   ├── Models/            # Domain entities
│   │   ├── Services/          # Business logic layer
│   │   ├── Program.cs         # DI + pipeline setup
│   │   └── appsettings.json
│   └── LeaveManagement.Tests/
│       └── LeaveServiceTests.cs   # 7 xUnit tests with Moq
├── README.md
├── SETUP.md
└── PROJECT_OVERVIEW.md
```

## Quick Start

See [SETUP.md](SETUP.md) for full setup instructions.

```bash
# Restore, migrate, and run
dotnet restore
dotnet ef database update --project src/LeaveManagement.API
dotnet run --project src/LeaveManagement.API
```

Open Swagger UI at: `https://localhost:7001/swagger`

## Running Tests

```bash
dotnet test src/LeaveManagement.Tests
```
