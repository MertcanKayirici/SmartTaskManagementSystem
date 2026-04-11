# 🧠 Database Structure

> This folder contains all database-related files for the **Smart Task Management System**.

---

## 🎯 Purpose

The database is designed to support:

- User-based task management  
- Category-task relationships  
- Audit logging for user actions  
- ASP.NET Core Identity integration  

---

## 🗺️ Database Diagram

Below is the relational database structure:

![Database Diagram](database_diagram.png)

---

## 🏗️ Architecture Approach

This project follows a **Code-First approach** using **Entity Framework Core**.

👉 The database schema is generated directly from application models.

---

## ⚙️ Recommended Setup (EF Core)

### 📦 Required NuGet Packages

Install the following packages:

- Microsoft.EntityFrameworkCore.SqlServer  
- Microsoft.EntityFrameworkCore.Tools  
- Microsoft.EntityFrameworkCore.Design  
- Microsoft.AspNetCore.Identity.EntityFrameworkCore  

---

### 🚀 Migration Commands

```powershell
Add-Migration InitialCreate
Update-Database
```

✔️ This will automatically create:

- ASP.NET Core Identity tables
- Application tables (TaskItems, Categories, AuditLogs)
- All relationships and constraints

---

## 🧾 Alternative Setup (Manual SQL)

> If required, the database can be created manually.

```bash
/DataBase/SmartTaskManagementDb.sql
```
> Located inside the DataBase folder of the project

✔️ This script includes:

- Identity tables
- Custom tables (TaskItems, Categories, AuditLogs)
- Foreign keys and constraints

---

## 🗂️ Core Tables

### 📋 TaskItems

Stores all tasks created by users.

### 🗂️ Categories

Stores user-defined task categories.

### 🧾 AuditLogs

Stores all system activity logs.

---

## 🔗 Relationships
- One user → many tasks
- One user → many categories
- One category → many tasks
- One task → optional category
- Audit logs → track user actions

---

## 🧠 Design Highlights
- User-based data isolation
- Normalized relational structure
- Scalable table design
- Audit logging for traceability
- Identity integration for security

---

## 📌 Notes
- Database: SQL Server
- ORM: Entity Framework Core
- Authentication: ASP.NET Core Identity
- Recommended method: Code-First (Migrations)

---

## 💡 Why This Matters

This database design reflects a real-world production scenario, not just a simple schema.

It demonstrates:

- Relational database design
- User-based architecture
- Data integrity with constraints
- Scalable backend structure

---

## 🚀 Summary

This database structure is designed to support a scalable, secure, and maintainable task management system.

It ensures:

- Clean data relationships  
- User-based data isolation  
- High data integrity  
- Extensibility for future features  

---
