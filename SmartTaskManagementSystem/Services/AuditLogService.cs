using SmartTaskManagementSystem.Data;
using SmartTaskManagementSystem.Models;

namespace SmartTaskManagementSystem.Services
{
    // Service responsible for recording system activities (audit logs)
    public class AuditLogService
    {
        // Database context (Entity Framework Core)
        private readonly ApplicationDbContext _context;

        // Constructor injection for DbContext (Dependency Injection)
        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Asynchronously creates and saves a new audit log entry
        public async Task LogAsync(
            string actionType,     // Type of action (Create, Update, Delete, etc.)
            string entityName,     // Entity name (Task, Category, etc.)
            int? entityId,         // Related entity ID (nullable)
            string description,    // Description of the action
            string? userId         // User who performed the action (nullable)
        )
        {
            // Create a new audit log object
            var log = new AuditLog
            {
                ActionType = actionType,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                UserId = userId,

                // Timestamp when the action occurred
                CreatedAt = DateTime.Now
            };

            // Add log to database
            _context.AuditLogs.Add(log);

            // Persist changes asynchronously
            await _context.SaveChangesAsync();
        }
    }
}