using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagementSystem.Data;
using SmartTaskManagementSystem.Models;
using SmartTaskManagementSystem.Services;

namespace SmartTaskManagementSystem.Controllers
{
    // Restricts all actions in this controller to authenticated users
    [Authorize]
    public class CategoryController : Controller
    {
        // Database context used for category and task operations
        private readonly ApplicationDbContext _context;

        // User manager used to access the currently authenticated user
        private readonly UserManager<IdentityUser> _userManager;

        // Audit log service used to record important category actions
        private readonly AuditLogService _auditLogService;

        // Injects required dependencies for category operations
        public CategoryController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            AuditLogService auditLogService)
        {
            _context = context;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }

        // Displays the category list page with optional filtering
        [HttpGet]
        public async Task<IActionResult> Index(string? status = null, bool linkedOnly = false, string? search = null)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Forces authentication challenge if user ID is missing
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // Builds the base query for categories belonging to the current user
            var categoriesQuery = _context.Categories
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            // Applies search filtering by category name
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchText = search.Trim();
                categoriesQuery = categoriesQuery.Where(x => x.Name.Contains(searchText));
            }

            // Applies status filtering when a specific status is selected
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                if (status == "Active")
                {
                    categoriesQuery = categoriesQuery.Where(x => x.IsActive);
                }
                else if (status == "Passive")
                {
                    categoriesQuery = categoriesQuery.Where(x => !x.IsActive);
                }
            }

            // Loads filtered categories in alphabetical order
            var categories = await categoriesQuery
                .OrderBy(x => x.Name)
                .ToListAsync();

            // Loads all user tasks to determine linked category usage
            var taskItems = await _context.TaskItems
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync();

            // Extracts distinct category IDs that are currently used by tasks
            var linkedCategoryIds = taskItems
                .Where(t => t.CategoryId.HasValue)
                .Select(t => t.CategoryId!.Value)
                .Distinct()
                .ToHashSet();

            // Applies linked-only filtering if requested
            if (linkedOnly)
            {
                categories = categories
                    .Where(c => linkedCategoryIds.Contains(c.Id))
                    .ToList();
            }

            // Passes initial filter values and linked category data to the view
            ViewBag.InitialStatus = string.IsNullOrWhiteSpace(status) ? "All" : status;
            ViewBag.InitialLinkedOnly = linkedOnly;
            ViewBag.InitialSearch = search ?? "";
            ViewBag.LinkedCategoryIds = linkedCategoryIds;

            // Passes additional category/task summary data to the view
            ViewBag.TotalTasksInCategories = taskItems.Count(x => x.CategoryId != null);
            ViewBag.CategoriesWithTasks = categories.Count(c => linkedCategoryIds.Contains(c.Id));

            return View(categories);
        }

        // Returns category summary data for AJAX-based dashboard/stat cards
        [HttpGet]
        public async Task<IActionResult> GetCategorySummary()
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Returns an error response if the user cannot be resolved
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Loads all categories belonging to the current user
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync();

            // Loads all tasks belonging to the current user
            var taskItems = await _context.TaskItems
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync();

            // Determines which categories are linked to tasks
            var linkedCategoryIds = taskItems
                .Where(t => t.CategoryId.HasValue)
                .Select(t => t.CategoryId!.Value)
                .Distinct()
                .ToHashSet();

            // Calculates summary values
            var total = categories.Count;
            var active = categories.Count(x => x.IsActive);
            var passive = categories.Count(x => !x.IsActive);
            var linked = categories.Count(c => linkedCategoryIds.Contains(c.Id));
            var unused = total - linked;

            // Finds the most used category based on task count
            var mostUsedCategory = categories
                .Select(c => new
                {
                    c.Name,
                    TaskCount = taskItems.Count(t => t.CategoryId == c.Id)
                })
                .OrderByDescending(x => x.TaskCount)
                .FirstOrDefault();

            // Builds a readable insight text for the UI
            var insightText = linked > 0
                ? $"{linked} categor{(linked == 1 ? "y is" : "ies are")} currently linked to tasks."
                : "No category is linked to tasks yet.";

            // Returns summary payload
            return Json(new
            {
                success = true,
                total,
                active,
                passive,
                linked,
                unused,
                insightText,
                mostUsedCategory = mostUsedCategory?.Name ?? "No category yet",
                mostUsedCategoryCount = mostUsedCategory?.TaskCount ?? 0
            });
        }

        // Displays the category creation form
        [HttpGet]
        public IActionResult Create()
        {
            // Creates a default model with initial values
            var model = new Category
            {
                IsActive = true,
                Color = "#6f42c1"
            };

            return View(model);
        }

        // Handles category creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Forces authentication challenge if user ID is missing
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // Assigns ownership of the category to the current user
            category.UserId = userId;

            // Applies simple color suggestion logic based on category name
            ApplySimpleCategoryAi(category);

            // Returns the form if validation fails
            if (!ModelState.IsValid)
                return View(category);

            // Saves the new category
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            // Writes an audit log record for category creation
            await _auditLogService.LogAsync(
                "Create",
                "Category",
                category.Id,
                $"Category created: {category.Name}",
                userId
            );

            TempData["SuccessMessage"] = "Category created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Displays the category edit form
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            // Returns 404 if the ID is missing
            if (id == null)
                return NotFound();

            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Loads the requested category only if it belongs to the current user
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            // Returns 404 if the category is not found
            if (category == null)
                return NotFound();

            return View(category);
        }

        // Handles category update operations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            // Prevents ID mismatch between route and posted model
            if (id != category.Id)
                return NotFound();

            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Forces authentication challenge if user ID is missing
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // Reassigns ownership explicitly for safety
            category.UserId = userId;

            // Applies simple color suggestion logic based on category name
            ApplySimpleCategoryAi(category);

            // Returns the form if validation fails
            if (!ModelState.IsValid)
                return View(category);

            // Loads the existing category that belongs to the current user
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            // Returns 404 if the category is not found
            if (existingCategory == null)
                return NotFound();

            // Updates editable fields
            existingCategory.Name = category.Name;
            existingCategory.Color = category.Color;
            existingCategory.IsActive = category.IsActive;

            await _context.SaveChangesAsync();

            // Writes an audit log record for category update
            await _auditLogService.LogAsync(
                "Update",
                "Category",
                existingCategory.Id,
                $"Category updated: {existingCategory.Name}",
                userId
            );

            TempData["SuccessMessage"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Returns tasks linked to a specific category for modal preview
        [HttpGet]
        public async Task<IActionResult> GetCategoryTasks(int id)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Loads all tasks linked to the selected category and current user
            var tasks = await _context.TaskItems
                .AsNoTracking()
                .Where(x => x.CategoryId == id && x.UserId == userId)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Status
                })
                .ToListAsync();

            return Json(tasks);
        }

        // Toggles category active/passive state using AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatusAjax(int id)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Returns an error if the user cannot be resolved
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "User not found." });

            // Loads the target category only if it belongs to the current user
            var category = await _context.Categories
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            // Returns an error if the category is missing
            if (category == null)
                return Json(new { success = false, message = "Category not found." });

            // Flips the active state
            category.IsActive = !category.IsActive;

            await _context.SaveChangesAsync();

            // Writes an audit log record for status change
            await _auditLogService.LogAsync(
                "Update",
                "Category",
                category.Id,
                $"Category status changed: {category.Name} -> {(category.IsActive ? "Active" : "Passive")}",
                userId
            );

            // Returns updated status information
            return Json(new
            {
                success = true,
                isActive = category.IsActive,
                statusText = category.IsActive ? "Active" : "Passive"
            });
        }

        // Deletes a category using AJAX, optionally deleting related tasks as well
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax([FromBody] CategoryDeleteRequest request)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Returns an error if the user cannot be resolved
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "User not found." });

            // Loads the category with related task items for the current user
            var category = await _context.Categories
                .Include(x => x.TaskItems)
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId);

            // Returns an error if the category is missing
            if (category == null)
                return Json(new { success = false, message = "Category not found." });

            var categoryName = category.Name;

            // Handles related tasks before deleting the category
            if (category.TaskItems != null && category.TaskItems.Any())
            {
                if (request.DeleteTasks)
                {
                    // Deletes all related tasks together with the category
                    _context.TaskItems.RemoveRange(category.TaskItems);
                }
                else
                {
                    // Keeps tasks and only removes the category link
                    foreach (var task in category.TaskItems)
                    {
                        task.CategoryId = null;
                        task.UpdatedAt = DateTime.Now;
                    }
                }
            }

            // Removes the category itself
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            // Writes an audit log record for delete operation
            await _auditLogService.LogAsync(
                "Delete",
                "Category",
                request.Id,
                request.DeleteTasks
                    ? $"Category and related tasks deleted: {categoryName}"
                    : $"Category deleted, related tasks kept: {categoryName}",
                userId
            );

            // Returns success response for the client
            return Json(new
            {
                success = true,
                deletedId = request.Id,
                message = request.DeleteTasks
                    ? "Category and related tasks deleted."
                    : "Category deleted. Related tasks were kept."
            });
        }

        // Applies simple keyword-based color suggestions to a category
        private static void ApplySimpleCategoryAi(Category category)
        {
            // Exits if the category name is empty
            if (string.IsNullOrWhiteSpace(category.Name))
                return;

            // Normalizes the category name for keyword matching
            var name = category.Name.ToLowerInvariant();

            if (name.Contains("work") || name.Contains("business") || name.Contains("office"))
            {
                category.Color = "#0d6efd";
            }
            else if (name.Contains("personal") || name.Contains("home") || name.Contains("life"))
            {
                category.Color = "#6f42c1";
            }
            else if (name.Contains("study") || name.Contains("school") || name.Contains("education"))
            {
                category.Color = "#198754";
            }
            else if (name.Contains("urgent") || name.Contains("important") || name.Contains("critical"))
            {
                category.Color = "#dc3545";
            }
            else if (name.Contains("shopping") || name.Contains("market") || name.Contains("buy"))
            {
                category.Color = "#fd7e14";
            }
        }
    }

    // DTO used for AJAX-based category delete requests
    public class CategoryDeleteRequest
    {
        // Target category ID
        public int Id { get; set; }

        // Determines whether related tasks should also be deleted
        public bool DeleteTasks { get; set; }
    }
}