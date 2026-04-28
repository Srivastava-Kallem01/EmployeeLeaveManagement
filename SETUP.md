# Setup & Running Guide

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 7.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/7.0) |
| SQL Server | 2019+ | LocalDB or full instance |
| Visual Studio / VS Code | Any | VS Code + C# Dev Kit recommended |
| EF Core CLI | 7.x | `dotnet tool install --global dotnet-ef` |

---

## Step 1 — Clone / Open the project

```bash
cd EmployeeLeaveManagement
```

## Step 2 — Configure the database connection

Open `src/LeaveManagement.API/appsettings.json` and update the connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=LeaveManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

**For SQL Server LocalDB:**
```
Server=(localdb)\\mssqllocaldb;Database=LeaveManagementDb;Trusted_Connection=True;
```

**For SQL Server with credentials:**
```
Server=localhost,1433;Database=LeaveManagementDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

## Step 3 — Update the JWT secret

In `appsettings.json`, replace the JWT key with your own (must be 32+ characters):

```json
"Jwt": {
  "Key": "YourOwnSecretKeyAtLeast32CharactersLong!",
  "Issuer": "LeaveManagementAPI",
  "Audience": "LeaveManagementClient"
}
```

> **Never commit real secrets.** Use `appsettings.Development.json` or environment variables for production.

## Step 4 — Restore NuGet packages

```bash
dotnet restore
```

## Step 5 — Create and apply database migrations

```bash
# Install EF Core CLI (first time only)
dotnet tool install --global dotnet-ef

# Create migration (only needed if you modify models)
dotnet ef migrations add InitialCreate --project src/LeaveManagement.API

# Apply migration to the database
dotnet ef database update --project src/LeaveManagement.API
```

> The app also runs `db.Database.Migrate()` on startup, so migrations apply automatically when you run the API.

## Step 6 — Run the API

```bash
dotnet run --project src/LeaveManagement.API
```

Expected console output:
```
[12:00:00 INF] Now listening on: https://localhost:7001
[12:00:00 INF] Now listening on: http://localhost:5001
[12:00:00 INF] Leave Management API started in Development mode
```

## Step 7 — Open Swagger UI

Navigate to: **https://localhost:7001/swagger**

You will see all endpoints with request/response schemas and a built-in test client.

---

## Testing the API

### 1. Register an Admin account

```http
POST /api/auth/register
Content-Type: application/json

{
  "fullName": "Admin User",
  "email": "admin@company.com",
  "password": "Admin@1234",
  "department": "HR",
  "role": "Admin"
}
```

### 2. Register an Employee account

```http
POST /api/auth/register
{
  "fullName": "John Smith",
  "email": "john@company.com",
  "password": "Employee@123",
  "department": "Engineering",
  "role": "Employee"
}
```

### 3. Login and get JWT token

```http
POST /api/auth/login
{
  "email": "john@company.com",
  "password": "Employee@123"
}
```

Copy the `token` from the response.

### 4. Authorize in Swagger

Click **Authorize** button → enter: `Bearer <your-token>`

### 5. Submit a leave request

```http
POST /api/leave-requests
Authorization: Bearer <token>
{
  "startDate": "2025-06-01",
  "endDate": "2025-06-05",
  "leaveType": "Annual",
  "reason": "Family vacation"
}
```

### 6. Review as Admin/Manager

Login as Admin, then:

```http
PUT /api/leave-requests/1/review
Authorization: Bearer <admin-token>
{
  "status": "Approved",
  "managerComment": "Approved. Enjoy!"
}
```

---

## Running Unit Tests

```bash
dotnet test src/LeaveManagement.Tests --verbosity normal
```

Expected output:
```
Test summary: total: 7, failed: 0, succeeded: 7, skipped: 0
```

---

## Log Files

Logs are written to `src/LeaveManagement.API/logs/` as rolling daily files:
```
logs/leave-api-20250601.log
logs/leave-api-20250602.log
```

---

## Common Issues

| Problem | Fix |
|---|---|
| `Cannot open database` | Check connection string; ensure SQL Server is running |
| `401 Unauthorized` | Token expired or missing `Bearer ` prefix in header |
| `EF Core migration error` | Run `dotnet ef database update --project src/LeaveManagement.API` |
| `Port already in use` | Change port in `launchSettings.json` or kill the process |
