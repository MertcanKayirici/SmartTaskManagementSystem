using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagementSystem.Data;
using SmartTaskManagementSystem.Models;
using SmartTaskManagementSystem.Services;

namespace SmartTaskManagementSystem.Controllers
{
    // Restricts all task-related actions to authenticated users
    [Authorize]
    public class TaskController : Controller
    {
        // Database context used for task and category operations
        private readonly ApplicationDbContext _context;

        // Identity service used to access the current user
        private readonly UserManager<IdentityUser> _userManager;

        // Service used to store audit log records
        private readonly AuditLogService _auditLogService;

        // Initializes controller dependencies
        public TaskController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            AuditLogService auditLogService)
        {
            _context = context;
            _userManager = userManager;
            _auditLogService = auditLogService;
        }

        // Displays the main task list page with initial filter values
        [HttpGet]
        public async Task<IActionResult> Index(
            int? categoryId = null,
            string? statusFilter = null,
            string? priorityFilter = null,
            string? dateFilter = null,
            string? sortOrder = null,
            string? search = null)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Loads user tasks that belong to active categories
            var tasks = await _context.TaskItems
                .Where(x => x.UserId == userId)
                .Include(x => x.Category)
                .Where(x => x.Category != null && x.Category.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            // Loads active categories for the filter dropdown
            var categories = await _context.Categories
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            // Builds the category filter dropdown list
            ViewBag.CategoryFilterList = new SelectList(categories, "Id", "Name", categoryId);

            // Stores initial filter values for the view
            ViewBag.InitialCategoryId = categoryId;
            ViewBag.InitialStatusFilter = statusFilter ?? "All";
            ViewBag.InitialPriorityFilter = priorityFilter ?? "All";
            ViewBag.InitialDateFilter = dateFilter ?? "All";
            ViewBag.InitialSortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "newest" : sortOrder;
            ViewBag.InitialSearch = search ?? "";

            return View(tasks);
        }

        // Returns filtered task data for AJAX requests
        [HttpGet]
        public async Task<IActionResult> FilterAjax(
            string? search,
            string? statusFilter,
            int? categoryId,
            string? dateFilter,
            string? priorityFilter,
            string? sortOrder)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Creates the base query for active-category tasks
            var query = _context.TaskItems
                .Where(x => x.UserId == userId)
                .Include(x => x.Category)
                .Where(x => x.Category != null && x.Category.IsActive)
                .AsQueryable();

            // Applies keyword search against title and description
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchText = search.Trim();

                query = query.Where(x =>
                    x.Title.Contains(searchText) ||
                    (x.Description != null && x.Description.Contains(searchText)));
            }

            // Applies status filtering
            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                if (statusFilter == "PendingOpen")
                {
                    query = query.Where(x => x.Status != "Completed");
                }
                else
                {
                    query = query.Where(x => x.Status == statusFilter);
                }
            }

            // Applies category filtering
            if (categoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == categoryId.Value);
            }

            // Applies priority filtering
            if (!string.IsNullOrWhiteSpace(priorityFilter) && priorityFilter != "All")
            {
                query = query.Where(x => x.Priority == priorityFilter);
            }

            // Date reference values used by date-based filters
            var today = DateTime.Today;
            var weekEnd = today.AddDays(7);
            var monthEnd = today.AddMonths(1);

            // Applies date filtering
            if (!string.IsNullOrWhiteSpace(dateFilter) && dateFilter != "All")
            {
                switch (dateFilter)
                {
                    case "Overdue":
                        query = query.Where(x => x.DueDate.Date < today && x.Status != "Completed");
                        break;
                    case "Today":
                        query = query.Where(x => x.DueDate.Date == today);
                        break;
                    case "ThisWeek":
                        query = query.Where(x => x.DueDate.Date >= today && x.DueDate.Date <= weekEnd);
                        break;
                    case "ThisMonth":
                        query = query.Where(x => x.DueDate.Date >= today && x.DueDate.Date <= monthEnd);
                        break;
                }
            }

            // Applies sorting rules
            query = sortOrder switch
            {
                "oldest" => query.OrderBy(x => x.CreatedAt),
                "due_asc" => query.OrderBy(x => x.DueDate).ThenByDescending(x => x.CreatedAt),
                "due_desc" => query.OrderByDescending(x => x.DueDate).ThenByDescending(x => x.CreatedAt),
                "priority" => query
                    .OrderByDescending(x => x.Priority == "High" ? 3 : x.Priority == "Medium" ? 2 : 1)
                    .ThenBy(x => x.DueDate),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            // Executes the filtered query
            var tasks = await query.ToListAsync();

            // Maps task data into an AJAX-friendly response model
            var result = tasks.Select(item =>
            {
                var dueSoon = item.Status != "Completed" &&
                              item.DueDate.Date >= today &&
                              item.DueDate.Date <= today.AddDays(2);

                var overdue = item.Status != "Completed" &&
                              item.DueDate.Date < today;

                return new
                {
                    id = item.Id,
                    title = item.Title,
                    description = item.Description ?? "",
                    priority = item.Priority,
                    status = item.Status,
                    dueDate = item.DueDate.ToString("dd MMM yyyy"),
                    categoryId = item.CategoryId,
                    categoryName = item.Category != null ? item.Category.Name : "",
                    categoryColor = item.Category != null && !string.IsNullOrWhiteSpace(item.Category.Color)
                        ? item.Category.Color
                        : "#6c757d",
                    isCompleted = item.Status == "Completed",
                    dueSoon,
                    overdue
                };
            }).ToList();

            // Returns summary counts together with filtered task data
            return Json(new
            {
                success = true,
                total = tasks.Count,
                completed = tasks.Count(x => x.Status == "Completed"),
                pending = tasks.Count(x => x.Status != "Completed"),
                tasks = result
            });
        }

        // Displays the create task form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Loads active categories for the create form dropdown
            var categories = await _context.Categories
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewBag.CategoryId = new SelectList(categories, "Id", "Name");

            // Returns a default task model
            return View(new TaskItem
            {
                Status = "Pending",
                Priority = "Low",
                DueDate = DateTime.Today
            });
        }

        // Handles task creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem taskItem)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Redirects to login if the session is missing
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // Loads active categories again for validation fallback
            var categories = await _context.Categories
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            // Applies simple keyword-based priority suggestion
            ApplySimplePrioritySuggestion(taskItem);

            // Returns the form again if validation fails
            if (!ModelState.IsValid)
            {
                ViewBag.CategoryId = new SelectList(categories, "Id", "Name", taskItem.CategoryId);
                return View(taskItem);
            }

            // Assigns system-managed task fields
            taskItem.UserId = userId;
            taskItem.CreatedAt = DateTime.Now;
            taskItem.UpdatedAt = null;

            // Saves the new task
            _context.TaskItems.Add(taskItem);
            await _context.SaveChangesAsync();

            // Logs task creation
            await _auditLogService.LogAsync(
                "Create",
                "TaskItem",
                taskItem.Id,
                $"Task created: {taskItem.Title}",
                userId
            );

            TempData["SuccessMessage"] = "Task created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Displays the edit task form
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            // Returns 404 if task ID is missing
            if (id == null)
                return NotFound();

            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Loads the target task for the current user
            var taskItem = await _context.TaskItems
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            // Returns 404 if the task does not exist
            if (taskItem == null)
                return NotFound();

            // Loads active categories for the edit form dropdown
            var categories = await _context.Categories
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewBag.CategoryId = new SelectList(categories, "Id", "Name", taskItem.CategoryId);
            return View(taskItem);
        }

        // Handles task update operations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskItem taskItem)
        {
            // Prevents mismatched route/model IDs
            if (id != taskItem.Id)
                return NotFound();

            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Loads active categories again for validation fallback
            var categories = await _context.Categories
                .Where(x => x.UserId == userId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            // Applies simple keyword-based priority suggestion
            ApplySimplePrioritySuggestion(taskItem);

            // Returns the form again if validation fails
            if (!ModelState.IsValid)
            {
                ViewBag.CategoryId = new SelectList(categories, "Id", "Name", taskItem.CategoryId);
                return View(taskItem);
            }

            // Loads the existing task for the current user
            var existingTask = await _context.TaskItems
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            // Returns 404 if the task is not found
            if (existingTask == null)
                return NotFound();

            // Updates editable task fields
            existingTask.Title = taskItem.Title;
            existingTask.Description = taskItem.Description;
            existingTask.DueDate = taskItem.DueDate;
            existingTask.Priority = taskItem.Priority;
            existingTask.Status = taskItem.Status;
            existingTask.CategoryId = taskItem.CategoryId;
            existingTask.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Logs task update
            await _auditLogService.LogAsync(
                "Update",
                "TaskItem",
                existingTask.Id,
                $"Task updated: {existingTask.Title}",
                userId
            );

            TempData["SuccessMessage"] = "Task updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Toggles task status using AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatusAjax(int id)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Returns an error if the user session is missing
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User session not found." });
            }

            // Loads the task and its category for the current user
            var task = await _context.TaskItems
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            // Returns an error if the task does not exist
            if (task == null)
            {
                return Json(new { success = false, message = "Task not found." });
            }

            // Toggles between completed and pending states
            task.Status = task.Status == "Completed" ? "Pending" : "Completed";
            task.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Logs status update
            await _auditLogService.LogAsync(
                "Update",
                "TaskItem",
                task.Id,
                $"Task status changed to: {task.Status} - {task.Title}",
                userId
            );

            // Returns the updated task status
            return Json(new
            {
                success = true,
                id = task.Id,
                status = task.Status,
                statusText = task.Status == "Completed" ? "Completed" : "Open",
                message = "Task status updated successfully."
            });
        }

        // Deletes a task using AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            // Gets the current user's ID
            var userId = _userManager.GetUserId(User);

            // Returns an error if the user session is missing
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User session not found." });
            }

            // Loads the target task for the current user
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            // Returns an error if the task does not exist
            if (task == null)
            {
                return Json(new { success = false, message = "Task not found." });
            }

            var taskTitle = task.Title;

            // Removes the task from the database
            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();

            // Logs deletion
            await _auditLogService.LogAsync(
                "Delete",
                "TaskItem",
                id,
                $"Task deleted: {taskTitle}",
                userId
            );

            // Returns a success response
            return Json(new
            {
                success = true,
                deletedId = id,
                message = "Task deleted successfully."
            });
        }

        // Applies simple keyword-based priority suggestion rules
        private static void ApplySimplePrioritySuggestion(TaskItem taskItem)
        {
            // Skips processing if task title is empty
            if (string.IsNullOrWhiteSpace(taskItem.Title))
                return;

            // Normalizes the title for keyword matching
            var title = taskItem.Title.ToLowerInvariant();

            // High-priority keywords
            if (title.Contains("exam") || title.Contains("deadline") || title.Contains("urgent") || title.Contains("presentation"))
            {
                taskItem.Priority = "High";
            }
            // Medium-priority keywords
            else if (title.Contains("meeting") || title.Contains("project") || title.Contains("report"))
            {
                taskItem.Priority = "Medium";
            }
            // Low-priority keywords
            else if (title.Contains("shopping") || title.Contains("buy") || title.Contains("market"))
            {
                taskItem.Priority = "Low";
            }
        }
    }
}