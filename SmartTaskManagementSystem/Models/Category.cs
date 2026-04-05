using System.ComponentModel.DataAnnotations;

namespace SmartTaskManagementSystem.Models
{
    // Represents a task category used to group tasks
    // Each category belongs to a specific user
    public class Category
    {
        // Primary key
        public int Id { get; set; }

        // Category name (required, max 50 characters)
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        // Optional color value (used for UI representation, e.g., badges or cards)
        [StringLength(20)]
        public string? Color { get; set; }

        // Indicates whether the category is active or not
        // Default value is true (active)
        public bool IsActive { get; set; } = true;

        // Foreign key linking category to a specific user (Identity)
        public string? UserId { get; set; }

        // Navigation property: one category can have multiple tasks
        public ICollection<TaskItem>? TaskItems { get; set; }
    }
}