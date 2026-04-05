using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartTaskManagementSystem.Areas.Identity.Pages.Account.Manage
{
    // Handles profile management operations for the authenticated user
    public class IndexModel : PageModel
    {
        // Manages user-related operations
        private readonly UserManager<IdentityUser> _userManager;

        // Manages sign-in related operations
        private readonly SignInManager<IdentityUser> _signInManager;

        // Logs profile update and system events
        private readonly ILogger<IndexModel> _logger;

        // Constructor for dependency injection
        public IndexModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // Stores the current username of the authenticated user
        public string Username { get; set; } = string.Empty;

        // Binds form input values to the page model
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Stores temporary status messages across requests
        [TempData]
        public string? StatusMessage { get; set; }

        // Represents the input fields used in the profile form
        public class InputModel
        {
            // User phone number
            [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }
        }

        // Loads current user profile data into the page model
        private async Task LoadAsync(IdentityUser user)
        {
            // Gets the current username
            Username = await _userManager.GetUserNameAsync(user) ?? string.Empty;

            // Loads the current phone number into the input model
            Input = new InputModel
            {
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user)
            };
        }

        // Handles GET requests for the profile page
        public async Task<IActionResult> OnGetAsync()
        {
            // Gets the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Returns 404 if the user cannot be found
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Loads the user's current profile data
            await LoadAsync(user);
            return Page();
        }

        // Handles POST requests when the profile form is submitted
        public async Task<IActionResult> OnPostAsync()
        {
            // Gets the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Returns 404 if the user cannot be found
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Returns the page if form validation fails
            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Gets the current phone number from Identity
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            // Updates the phone number only if the new value is different
            if (Input.PhoneNumber != phoneNumber)
            {
                // Attempts to update the user's phone number
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);

                // Sets an error message if the update fails
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Telefon numarası güncellenirken bir hata oluştu.";
                    return RedirectToPage();
                }
            }

            // Refreshes the user's sign-in session after profile update
            await _signInManager.RefreshSignInAsync(user);

            // Logs successful profile update event
            _logger.LogInformation("Kullanıcı profil bilgilerini güncelledi.");

            // Sets success message for display after redirect
            StatusMessage = "Profiliniz başarıyla güncellendi.";
            return RedirectToPage();
        }
    }
}