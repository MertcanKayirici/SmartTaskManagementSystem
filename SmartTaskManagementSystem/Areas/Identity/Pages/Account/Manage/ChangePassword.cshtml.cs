using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartTaskManagementSystem.Areas.Identity.Pages.Account.Manage
{
    // Handles password change operations for the authenticated user
    public class ChangePasswordModel : PageModel
    {
        // Manages user-related operations
        private readonly UserManager<IdentityUser> _userManager;

        // Manages sign-in related operations
        private readonly SignInManager<IdentityUser> _signInManager;

        // Logs password change and system events
        private readonly ILogger<ChangePasswordModel> _logger;

        // Constructor for dependency injection
        public ChangePasswordModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // Binds form input values to the page model
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Stores temporary status messages across requests
        [TempData]
        public string? StatusMessage { get; set; }

        // Represents the input fields used in the change password form
        public class InputModel
        {
            // Current password entered by the user
            [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string OldPassword { get; set; } = string.Empty;

            // New password entered by the user
            [Required(ErrorMessage = "Yeni şifre zorunludur.")]
            [StringLength(100, ErrorMessage = "Şifre en az {2} karakter olmalıdır.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; } = string.Empty;

            // Confirmation field for the new password
            [Required(ErrorMessage = "Şifre tekrar zorunludur.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // Handles GET requests for the change password page
        public async Task<IActionResult> OnGetAsync()
        {
            // Gets the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Returns 404 if the user cannot be found
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Checks whether the user already has a password
            var hasPassword = await _userManager.HasPasswordAsync(user);

            // Redirects to set password page if no password exists
            if (!hasPassword)
            {
                return RedirectToPage("./SetPassword");
            }

            return Page();
        }

        // Handles POST requests when the change password form is submitted
        public async Task<IActionResult> OnPostAsync()
        {
            // Returns the page if form validation fails
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Gets the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Returns 404 if the user cannot be found
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Attempts to change the user's password
            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user,
                Input.OldPassword,
                Input.NewPassword);

            // Adds Identity errors to model state if password change fails
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

            // Refreshes the user's sign-in session after successful password update
            await _signInManager.RefreshSignInAsync(user);

            // Logs successful password change event
            _logger.LogInformation("Kullanıcı şifresini başarıyla değiştirdi.");

            // Sets success message for display after redirect
            StatusMessage = "Şifreniz başarıyla güncellendi.";
            return RedirectToPage();
        }
    }
}