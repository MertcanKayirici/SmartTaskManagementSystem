using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagementSystem.Models;

namespace SmartTaskManagementSystem.Controllers
{
    // Handles public-facing pages such as Home, Privacy, and Error
    public class HomeController : Controller
    {
        // Logger instance used for diagnostic and application logging
        private readonly ILogger<HomeController> _logger;

        // Injects the logger dependency into the controller
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Displays the main landing page
        public IActionResult Index()
        {
            // Sets the page title for the home view
            ViewData["Title"] = "Smart Task Management";
            return View();
        }

        // Displays the privacy policy page
        public IActionResult Privacy()
        {
            // Sets the page title for the privacy view
            ViewData["Title"] = "Privacy Policy";
            return View();
        }

        // Displays the application error page and disables response caching
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Passes the current request ID to the error view for tracing
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}