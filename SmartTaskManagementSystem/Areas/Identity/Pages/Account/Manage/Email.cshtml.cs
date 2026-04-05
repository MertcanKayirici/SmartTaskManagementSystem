using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace SmartTaskManagementSystem.Areas.Identity.Pages.Account.Manage
{
    // Handles email management operations for the authenticated user
    public class EmailModel : PageModel
    {
        // Manages user-related operations
        private readonly UserManager<IdentityUser> _userManager;

        // Manages sign-in related operations
        private readonly SignInManager<IdentityUser> _signInManager;

        // Logs email update and verification events
        private readonly ILogger<EmailModel> _logger;

        // Constructor for dependency injection
        public EmailModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<EmailModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // Stores the current email address of the user
        public string Email { get; set; } = string.Empty;

        // Indicates whether the current email address is confirmed
        public bool IsEmailConfirmed { get; set; }

        // Binds form input values to the page model
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Stores temporary status messages across requests
        [TempData]
        public string? StatusMessage { get; set; }

        // Represents the input fields used in the email update form
        public class InputModel
        {
            // New email address entered by the user
            [Required(ErrorMessage = "Yeni email zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli bir email giriniz.")]
            [Display(Name = "New email")]
            public string NewEmail { get; set; } = string.Empty;
        }

        // Loads current user email data into the page model
        private async Task LoadAsync(IdentityUser user)
        {
            // Gets the current email address
            Email = await _userManager.GetEmailAsync(user) ?? string.Empty;

            // Pre-fills the form input with the current email address
            Input = new InputModel
            {
                NewEmail = Email
            };

            // Checks whether the email address is confirmed
            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        // Handles GET requests for the email management page
        public async Task<IActionResult> OnGetAsync()
        {
            // Gets the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Returns 404 if the user cannot be found
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Loads the user's current email information
            await LoadAsync(user);
            return Page();
        }

        // Handles POST requests for changing the email address
        public async Task<IActionResult> OnPostChangeEmailAsync()
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

            // Gets the current email address from Identity
            var email = await _userManager.GetEmailAsync(user);

            // Updates the email only if the new value is different
            if (Input.NewEmail != email)
            {
                // Attempts to update the user's email address
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.NewEmail);

                // Adds Identity errors to model state if email update fails
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await LoadAsync(user);
                    return Page();
                }

                // Updates the username to match the new email address
                await _userManager.SetUserNameAsync(user, Input.NewEmail);

                // Refreshes the user's sign-in session after successful update
                await _signInManager.RefreshSignInAsync(user);

                // Logs successful email update event
                _logger.LogInformation("Kullanıcı email adresini güncelledi.");

                // Sets success message for display after redirect
                StatusMessage = "Email adresiniz başarıyla güncellendi.";
                return RedirectToPage();
            }

            // Sets info message if the new email matches the current email
            StatusMessage = "Yeni email adresi mevcut email ile aynı.";
            return RedirectToPage();
        }

        // Handles POST requests for sending a verification email
        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            // Gets the currently authenticated user
            var user = await _userManager.GetUserAsync(User);

            // Returns 404 if the user cannot be found
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Reloads user email data into the page model
            await LoadAsync(user);

            // Gets user identifier and current email address
            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);

            // Generates email confirmation token
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Encodes the token for safe use in the URL
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            // Creates email confirmation callback URL
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId, code },
                protocol: Request.Scheme);

            // Logs the generated verification link
            _logger.LogInformation("Email doğrulama bağlantısı oluşturuldu: {CallbackUrl}", HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty));

            // Sets success message after sending verification request
            StatusMessage = "Doğrulama emaili tekrar gönderildi.";
            return RedirectToPage();
        }
    }
}