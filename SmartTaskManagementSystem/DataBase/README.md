# Database Structure

This folder contains the database-related files of the Smart Task Management System project.

---

## Purpose

The database is designed to support:

- User-based task management
- Category-task relationships
- Audit logging for user actions
- ASP.NET Core Identity integration

---

## Database Diagram

Below is the database structure diagram:

![Database Diagram](database_diagram.png)

---

## Recommended Approach (EF Core + NuGet)

The database **should be created using Entity Framework Core migrations**.

This project follows a **Code-First approach**, meaning the database is generated from the models.

### Required NuGet Packages

Install the following packages:

- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools
- Microsoft.EntityFrameworkCore.Design
- Microsoft.AspNetCore.Identity.EntityFrameworkCore

---

### Steps

```powershell
Add-Migration InitialCreate
Update-Database
```

This will automatically create:

- Identity tables
- Application tables (TaskItems, Categories, AuditLogs)

---

## Alternative Approach (Manual SQL)

If needed, the database can also be created manually using SQL.

📌 The full SQL script is available in:
```
SmartTaskManagementDb.sql
```

This file includes:

- All ASP.NET Identity tables
- All custom tables (TaskItems, Categories, AuditLogs)
- Relationships and constraints

--- 
 
## Main Tables
### TaskItems

Stores all task records created by users.

### Categories

Stores task categories for each user.

### AuditLogs

Stores system activity logs.

---

## Relationships
- One user can have many tasks
- One user can have many categories
- One category can have many tasks
- One task optionally belongs to one category
- Audit logs track user actions

---

## Notes
- The project uses ASP.NET Core Identity
- Database: SQL Server
- Recommended method: Entity Framework Core (Code-First)
- SQL script is provided for manual setup if needed