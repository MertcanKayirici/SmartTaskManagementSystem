using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTaskManagementSystem.Models
{
    // Represents a task entity in the system
    // Each task belongs to a user and optionally to a category
    public class TaskItem
    {
        // Primary key
        public int Id { get; set; }

        // Task title (required, max 100 characters)
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        // Optional task description (max 500 characters)
        [StringLength(500)]
        public string? Description { get; set; }

        // Due date of the task (required)
        [Required]
        public DateTime DueDate { get; set; }

        // Task priority (Low, Medium, High)
        // Default is Medium
        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        // Task status (Pending, In Progress, Completed)
        // Default is Pending
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        // Foreign key to Category (optional)
        public int? CategoryId { get; set; }

        // Foreign key to Identity user (task ownership)
        public string? UserId { get; set; }

        // Creation timestamp (set when task is created)
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Last update timestamp (nullable)
        public DateTime? UpdatedAt { get; set; }

        // Navigation property to Category
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}