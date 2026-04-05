using System.ComponentModel.DataAnnotations;

namespace SmartTaskManagementSystem.Models
{
    // Represents a system audit log entry
    // Used to track user actions such as Create, Update, Delete, etc.
    public class AuditLog
    {
        // Primary key
        public int Id { get; set; }

        // Type of action performed (e.g., Create, Update, Delete)
        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = null!;

        // Name of the affected entity (e.g., Task, Category)
        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = null!;

        // Optional ID of the related entity
        public int? EntityId { get; set; }

        // Optional detailed description of the action
        [StringLength(500)]
        public string? Description { get; set; }

        // ID of the user who performed the action (from Identity system)
        public string? UserId { get; set; }

        // Timestamp when the action occurred
        // Default value is set at object creation time
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}