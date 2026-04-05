using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagementSystem.Models;

namespace SmartTaskManagementSystem.Data
{
    // Main database context of the application
    // Inherits from IdentityDbContext to include ASP.NET Core Identity tables (Users, Roles, etc.)
    public class ApplicationDbContext : IdentityDbContext
    {
        // Constructor with dependency injection for DbContext options
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Represents the Tasks table in the database
        public DbSet<TaskItem> TaskItems { get; set; }

        // Represents the Categories table in the database
        public DbSet<Category> Categories { get; set; }

        // Represents the AuditLogs table in the database
        public DbSet<AuditLog> AuditLogs { get; set; }
    }
}