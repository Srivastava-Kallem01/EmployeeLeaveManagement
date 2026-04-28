# Project Overview — Employee Leave Management System

## What This Project Is

A backend REST API that handles the full lifecycle of employee leave requests inside an organization. Employees submit leave requests, managers approve or reject them, and the system enforces business rules automatically (balance tracking, overlap detection, date validation).

This project reflects work done at **Amplee Logic** — building backend services with ASP.NET Core, Entity Framework Core, JWT-based RBAC, SQL Server optimization, and xUnit testing.

---

## Architecture

```
┌────────────────────────────────────────────────────┐
│                  HTTP Client / Swagger              │
└──────────────────────┬─────────────────────────────┘
                       │
┌──────────────────────▼─────────────────────────────┐
│             ASP.NET Core Web API (.NET 7)           │
│                                                     │
│  ┌─────────────┐   ┌──────────────────────────┐    │
│  │  Middleware │   │       Controllers         │    │
│  │ (Exception) │   │  AuthController           │    │
│  └─────────────┘   │  LeaveRequestController   │    │
│                    └────────────┬─────────────┘    │
│                                 │                   │
│                    ┌────────────▼─────────────┐    │
│                    │        Services           │    │
│                    │  AuthService (JWT issue)  │    │
│                    │  LeaveService (business)  │    │
│                    └────────────┬─────────────┘    │
│                                 │                   │
│                    ┌────────────▼─────────────┐    │
│                    │   Entity Framework Core   │    │
│                    │       AppDbContext         │    │
│                    └────────────┬─────────────┘    │
└─────────────────────────────────┼──────────────────┘
                                  │
                    ┌─────────────▼──────────┐
                    │   SQL Server Database   │
                    │   LeaveManagementDb     │
                    └────────────────────────┘
```

---

## Database Schema

### Employees table

| Column | Type | Notes |
|---|---|---|
| Id | int (PK) | Auto-increment |
| FullName | nvarchar | |
| Email | nvarchar | Unique index |
| PasswordHash | nvarchar | BCrypt hash |
| Department | nvarchar | |
| Role | nvarchar | Admin / Manager / Employee |
| AnnualLeaveBalance | int | Default: 20 days |
| CreatedAt | datetime2 | UTC |

### LeaveRequests table

| Column | Type | Notes |
|---|---|---|
| Id | int (PK) | Auto-increment |
| EmployeeId | int (FK) | → Employees |
| StartDate | datetime2 | |
| EndDate | datetime2 | |
| LeaveType | nvarchar | Annual / Sick / Unpaid |
| Reason | nvarchar(500) | |
| Status | nvarchar | Pending / Approved / Rejected |
| ManagerComment | nvarchar(500) | Nullable |
| RequestedAt | datetime2 | UTC |
| ReviewedAt | datetime2 | Nullable, UTC |

**Indexes:** `Email` (unique on Employees), `Status` + `EmployeeId` (on LeaveRequests)

---

## Key Design Decisions

### 1. Service layer for all business logic
Controllers are thin — they only parse HTTP input and delegate to services. This keeps the business rules testable without spinning up a web server.

### 2. BCrypt for password hashing
Passwords are never stored in plain text. BCrypt adds a per-password salt automatically, making rainbow table attacks infeasible.

### 3. JWT with role claims
On login, the token encodes the employee's `Id`, `Email`, `FullName`, `Role`, and `Department`. No database lookup is needed per request — the API validates the token signature and reads claims directly.

### 4. Overlap detection before inserting
Before saving a new leave request, the service queries for any non-rejected request in the same date range for that employee. This prevents duplicate or conflicting bookings at the database level.

### 5. Balance deducted on approval, not submission
Leave balance is only reduced when a Manager/Admin sets status to `Approved`. If rejected, the full balance is preserved. Sick and Unpaid leave types do not affect the annual balance.

### 6. EF Core migrations applied on startup
`db.Database.Migrate()` in `Program.cs` means the schema is always in sync when the app starts — no manual migration scripts needed in CI/CD.

### 7. Global exception middleware
Instead of try/catch in every controller, a single `ExceptionMiddleware` catches `InvalidOperationException` (business errors → 400) and all unhandled exceptions (→ 500). This ensures consistent JSON error shapes across all endpoints.

---

## API Flow Walkthrough

```
1. Employee registers  →  POST /api/auth/register
2. Employee logs in   →  POST /api/auth/login  →  gets JWT token
3. Employee submits   →  POST /api/leave-requests  (with Bearer token)
   - Service validates dates, balance, overlaps
   - Saves LeaveRequest with Status = "Pending"
4. Manager retrieves  →  GET /api/leave-requests/pending  (Manager/Admin token)
5. Manager reviews    →  PUT /api/leave-requests/{id}/review
   - Status set to Approved/Rejected
   - Balance deducted if Approved + Annual type
   - ReviewedAt timestamp saved
```

---

## Testing Strategy

Tests are in `LeaveManagement.Tests` using **xUnit + Moq + EF InMemory provider**.

Each test creates an isolated in-memory database with a unique name (using `Guid.NewGuid()`), so tests run in parallel without interfering with each other.

**Covered scenarios:**
- Happy path: valid leave request creation
- Insufficient balance rejection
- Past start date rejection
- Overlapping dates rejection
- Approval deducts correct balance
- Rejection preserves balance
- Review of non-existent request returns false
- Employee can only retrieve their own requests

**Not tested here (integration test scope):**
- JWT token validation (handled by ASP.NET Core middleware)
- Role-based route access
- Database-level unique constraints

---

## Skills Demonstrated

| Skill | Where Applied |
|---|---|
| ASP.NET Core Web API | All controllers + middleware |
| Entity Framework Core + LINQ | AppDbContext, Services |
| SQL Server (via EF) | Schema design, indexing |
| JWT Authentication | AuthService, Program.cs |
| Role-Based Access Control | `[Authorize(Roles)]` on controller actions |
| Dependency Injection | All services registered in Program.cs |
| Serilog | Structured logging throughout services |
| Swagger/OpenAPI | Full API documentation with auth support |
| xUnit + Moq | LeaveServiceTests (7 test cases) |
| Clean Architecture | Controller → Service → Repository pattern |
