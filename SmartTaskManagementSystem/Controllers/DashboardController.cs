using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTaskManagementSystem.Data;

namespace SmartTaskManagementSystem.Controllers
{
    // Restricts dashboard access to authenticated users only
    [Authorize]
    public class DashboardController : Controller
    {
        // Database context used for querying dashboard-related data
        private readonly ApplicationDbContext _context;

        // User manager used to resolve the currently authenticated user
        private readonly UserManager<IdentityUser> _userManager;

        // Injects required services into the controller
        public DashboardController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Displays the main dashboard page with summary metrics and focus task cards
        [HttpGet]
        public async Task<IActionResult> Index(string? focusStatus = "Open", int focusCount = 6)
        {
            // Retrieves the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Redirects to authentication challenge if user cannot be resolved
            if (user == null)
            {
                return Challenge();
            }

            var userId = user.Id;
            var today = DateTime.Today;

            // Normalizes the allowed focus card counts
            if (focusCount != 6 && focusCount != 9 && focusCount != 12)
            {
                focusCount = 6;
            }

            // Calculates the start of the current week
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                startOfWeek = today.AddDays(-6);
            }

            // Loads all categories for the current user
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            // Loads task items that belong to active categories only
            var tasks = await _context.TaskItems
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.CategoryId != null)
                .Include(x => x.Category)
                .Where(x => x.Category != null && x.Category.IsActive)
                .ToListAsync();

            // Loads the latest audit log entries for the current user
            var latestAuditLogs = await _context.AuditLogs
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(3)
                .ToListAsync();

            // General task summary metrics
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(x => x.Status == "Completed");
            var pendingTasks = tasks.Count(x => x.Status != "Completed");

            // Priority-based task distribution
            var highPriorityTasks = tasks.Count(x => x.Priority == "High");
            var mediumPriorityTasks = tasks.Count(x => x.Priority == "Medium");
            var lowPriorityTasks = tasks.Count(x => x.Priority == "Low");

            // Deadline-related metrics
            var overdueTasks = tasks.Count(x => x.Status != "Completed" && x.DueDate.Date < today);
            var todayTasks = tasks.Count(x => x.DueDate.Date == today);
            var approachingDeadlines = tasks.Count(x => x.Status != "Completed" && x.DueDate.Date >= today && x.DueDate.Date <= today.AddDays(2));

            // Category-related metrics
            var activeCategories = categories.Count(x => x.IsActive);
            var categoriesWithTasks = categories.Count(c => tasks.Any(t => t.CategoryId == c.Id));

            // Weekly activity metrics
            var createdThisWeek = tasks.Count(x => x.CreatedAt.Date >= startOfWeek && x.CreatedAt.Date <= today);
            var dueThisWeek = tasks.Count(x => x.Status != "Completed" && x.DueDate.Date >= today && x.DueDate.Date <= startOfWeek.AddDays(6));

            // Finds the most frequently used category
            var mostUsedCategory = tasks
                .Where(x => x.Category != null)
                .GroupBy(x => x.Category!.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            // Loads the most recently created tasks
            var recentTasks = tasks
                .OrderByDescending(x => x.CreatedAt)
                .Take(4)
                .ToList();

            // Loads upcoming incomplete tasks ordered by due date and priority
            var upcomingTasks = tasks
                .Where(x => x.Status != "Completed")
                .OrderBy(x => x.DueDate)
                .ThenByDescending(x => x.Priority == "High" ? 3 : x.Priority == "Medium" ? 2 : 1)
                .Take(4)
                .ToList();

            // Starts building the focus-task query from in-memory task data
            var focusTasksQuery = tasks.AsEnumerable();

            // Applies optional focus-status filtering
            if (!string.IsNullOrWhiteSpace(focusStatus) && focusStatus != "All")
            {
                if (focusStatus == "Completed")
                {
                    focusTasksQuery = focusTasksQuery.Where(x => x.Status == "Completed");
                }
                else if (focusStatus == "Open")
                {
                    focusTasksQuery = focusTasksQuery.Where(x => x.Status != "Completed");
                }
            }

            // Selects highlighted task cards for the dashboard focus section
            var highlightTaskCards = focusTasksQuery
                .OrderByDescending(x => x.Priority == "High" ? 3 : x.Priority == "Medium" ? 2 : 1)
                .ThenBy(x => x.Status == "Completed" ? 1 : 0)
                .ThenBy(x => x.DueDate)
                .Take(focusCount)
                .ToList();

            // Builds category distribution data for charts or visual summaries
            var categoryDistribution = categories
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    Name = c.Name,
                    Color = string.IsNullOrWhiteSpace(c.Color) ? "#6f42c1" : c.Color,
                    Count = tasks.Count(t => t.CategoryId == c.Id)
                })
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .ToList();

            // Summary values passed to the dashboard view
            ViewBag.Total = totalTasks;
            ViewBag.Completed = completedTasks;
            ViewBag.Pending = pendingTasks;

            ViewBag.HighPriority = highPriorityTasks;
            ViewBag.MediumPriority = mediumPriorityTasks;
            ViewBag.LowPriority = lowPriorityTasks;

            ViewBag.OverdueTasks = overdueTasks;
            ViewBag.TodayTasks = todayTasks;

            ViewBag.ActiveCategories = activeCategories;
            ViewBag.CategoriesWithTasks = categoriesWithTasks;

            ViewBag.CreatedThisWeek = createdThisWeek;
            ViewBag.DueThisWeek = dueThisWeek;
            ViewBag.ApproachingDeadlines = approachingDeadlines;
            ViewBag.MostUsedCategory = mostUsedCategory?.Name ?? "No category yet";
            ViewBag.MostUsedCategoryCount = mostUsedCategory?.Count ?? 0;

            ViewBag.RecentTasks = recentTasks;
            ViewBag.UpcomingTasks = upcomingTasks;
            ViewBag.HighlightTaskCards = highlightTaskCards;
            ViewBag.CategoryDistribution = categoryDistribution;
            ViewBag.LatestAuditLogs = latestAuditLogs;

            ViewBag.FocusStatus = string.IsNullOrWhiteSpace(focusStatus) ? "Open" : focusStatus;
            ViewBag.FocusCount = focusCount;

            // Generates a simple smart insight message for the dashboard
            ViewBag.SmartInsight = BuildSmartInsight(
                totalTasks,
                completedTasks,
                approachingDeadlines,
                overdueTasks,
                mostUsedCategory?.Name,
                mostUsedCategory?.Count ?? 0);

            return View();
        }

        // Returns focus-task cards as JSON for AJAX-based dashboard updates
        [HttpGet]
        public async Task<IActionResult> GetFocusTasks(string? focusStatus = "Open", int focusCount = 6)
        {
            // Retrieves the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Returns an error response if user cannot be resolved
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Normalizes the allowed focus card counts
            if (focusCount != 6 && focusCount != 9 && focusCount != 12)
            {
                focusCount = 6;
            }

            var userId = user.Id;
            var today = DateTime.Today;

            // Loads task items that belong to active categories only
            var tasks = await _context.TaskItems
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.CategoryId != null)
                .Include(x => x.Category)
                .Where(x => x.Category != null && x.Category.IsActive)
                .ToListAsync();

            // Starts building the focus-task query from loaded task data
            var focusTasksQuery = tasks.AsEnumerable();

            // Applies optional focus-status filtering
            if (!string.IsNullOrWhiteSpace(focusStatus) && focusStatus != "All")
            {
                if (focusStatus == "Completed")
                {
                    focusTasksQuery = focusTasksQuery.Where(x => x.Status == "Completed");
                }
                else if (focusStatus == "Open")
                {
                    focusTasksQuery = focusTasksQuery.Where(x => x.Status != "Completed");
                }
            }

            // Projects focus task data into a JSON-friendly structure
            var result = focusTasksQuery
                .OrderByDescending(x => x.Priority == "High" ? 3 : x.Priority == "Medium" ? 2 : 1)
                .ThenBy(x => x.Status == "Completed" ? 1 : 0)
                .ThenBy(x => x.DueDate)
                .Take(focusCount)
                .Select(task => new
                {
                    id = task.Id,
                    title = task.Title,
                    description = string.IsNullOrWhiteSpace(task.Description) ? "No description available." : task.Description,
                    priority = task.Priority,
                    status = task.Status,
                    dueDate = task.DueDate.ToString("dd MMM yyyy"),
                    categoryName = task.Category != null ? task.Category.Name : "",
                    categoryColor = task.Category != null && !string.IsNullOrWhiteSpace(task.Category.Color)
                        ? task.Category.Color
                        : "#e9ecef",
                    isOverdue = task.Status != "Completed" && task.DueDate.Date < today,
                    isDueSoon = task.Status != "Completed" && task.DueDate.Date >= today && task.DueDate.Date <= today.AddDays(2)
                })
                .ToList();

            return Json(new
            {
                success = true,
                tasks = result
            });
        }

        // Returns dashboard summary metrics as JSON for live updates
        [HttpGet]
        public async Task<IActionResult> GetDashboardSummary()
        {
            // Retrieves the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Returns an error response if user cannot be resolved
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var userId = user.Id;
            var today = DateTime.Today;

            // Calculates the start of the current week
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday)
            {
                startOfWeek = today.AddDays(-6);
            }

            // Loads all categories for the current user
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync();

            // Loads task items that belong to active categories only
            var tasks = await _context.TaskItems
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.CategoryId != null)
                .Include(x => x.Category)
                .Where(x => x.Category != null && x.Category.IsActive)
                .ToListAsync();

            // Core dashboard metrics
            var total = tasks.Count;
            var completed = tasks.Count(x => x.Status == "Completed");
            var pending = tasks.Count(x => x.Status != "Completed");
            var completionRate = total > 0 ? (int)Math.Round(((double)completed / total) * 100) : 0;

            // Priority and deadline metrics
            var highPriority = tasks.Count(x => x.Priority == "High");
            var overdueTasks = tasks.Count(x => x.Status != "Completed" && x.DueDate.Date < today);
            var todayTasks = tasks.Count(x => x.DueDate.Date == today);
            var activeCategories = categories.Count(x => x.IsActive);
            var categoriesWithTasks = categories.Count(c => tasks.Any(t => t.CategoryId == c.Id));

            // Weekly dashboard metrics
            var createdThisWeek = tasks.Count(x => x.CreatedAt.Date >= startOfWeek && x.CreatedAt.Date <= today);
            var dueThisWeek = tasks.Count(x => x.Status != "Completed" && x.DueDate.Date >= today && x.DueDate.Date <= startOfWeek.AddDays(6));
            var approachingDeadlines = tasks.Count(x => x.Status != "Completed" && x.DueDate.Date >= today && x.DueDate.Date <= today.AddDays(2));

            // Finds the most frequently used category
            var mostUsedCategory = tasks
                .Where(x => x.Category != null)
                .GroupBy(x => x.Category!.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            // Builds a dashboard insight message
            var smartInsight = BuildSmartInsight(
                total,
                completed,
                approachingDeadlines,
                overdueTasks,
                mostUsedCategory?.Name,
                mostUsedCategory?.Count ?? 0);

            // Returns the full dashboard summary payload
            return Json(new
            {
                success = true,
                total,
                completed,
                pending,
                completionRate,
                highPriority,
                overdueTasks,
                todayTasks,
                activeCategories,
                categoriesWithTasks,
                createdThisWeek,
                dueThisWeek,
                approachingDeadlines,
                mostUsedCategory = mostUsedCategory?.Name ?? "No category yet",
                mostUsedCategoryCount = mostUsedCategory?.Count ?? 0,
                insightText = smartInsight
            });
        }

        // Generates a lightweight smart insight string for the dashboard
        private static string BuildSmartInsight(
            int totalTasks,
            int completedTasks,
            int approachingDeadlines,
            int overdueTasks,
            string? mostUsedCategory,
            int mostUsedCategoryCount)
        {
            // Prioritizes overdue work in the insight text
            if (overdueTasks > 0)
            {
                return $"You have {overdueTasks} overdue task(s). This should be your first focus.";
            }

            // Highlights approaching deadlines next
            if (approachingDeadlines > 0)
            {
                return $"You have {approachingDeadlines} task(s) close to deadline.";
            }

            // Encourages the user when all tasks are completed
            if (totalTasks > 0 && completedTasks == totalTasks)
            {
                return "Great job. All current tasks are completed.";
            }

            // Mentions the most active category when available
            if (!string.IsNullOrWhiteSpace(mostUsedCategory) && mostUsedCategoryCount > 0)
            {
                return $"You are most active in {mostUsedCategory} category.";
            }

            // Handles the empty-state scenario
            if (totalTasks == 0)
            {
                return "No tasks yet. Start by creating your first task.";
            }

            // Default balanced-state insight
            return "Your dashboard is up to date and looks balanced.";
        }
    }
}