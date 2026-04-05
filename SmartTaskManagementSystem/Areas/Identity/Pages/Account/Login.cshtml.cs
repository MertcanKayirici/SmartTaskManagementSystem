using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartTaskManagementSystem.Areas.Identity.Pages.Account
{
    // Handles user login operations for the Identity login page
    public class LoginModel : PageModel
    {
        // Manages sign-in related operations
        private readonly SignInManager<IdentityUser> _signInManager;

        // Manages user-related operations
        private readonly UserManager<IdentityUser> _userManager;

        // Logs authentication and system events
        private readonly ILogger<LoginModel> _logger;

        // Constructor for dependency injection
        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        // Binds login form input values to the model
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Stores available external authentication providers
        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        // Stores temporary error messages between requests
        [TempData]
        public string? ErrorMessage { get; set; }

        // Represents the user input fields on the login form
        public class InputModel
        {
            // User email address
            [Required(ErrorMessage = "Email alanı zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
            public string Email { get; set; } = string.Empty;

            // User password
            [Required(ErrorMessage = "Şifre alanı zorunludur.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            // Indicates whether the user wants to stay signed in
            [Display(Name = "Beni hatırla")]
            public bool RememberMe { get; set; }
        }

        // Handles GET requests for the login page
        public async Task OnGetAsync(string? returnUrl = null)
        {
            // Adds any temp error message to the model state
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            // Clears any existing external authentication cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Loads configured external login providers
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        // Handles POST requests when the login form is submitted
        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            // Sets the default return URL if none is provided
            returnUrl ??= Url.Content("~/");

            // Reloads external login providers for the page
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Returns the page if form validation fails
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Finds the user by email address
            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Returns an error if the user does not exist
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email veya şifre hatalı.");
                return Page();
            }

            // Attempts to sign in using the user's username and password
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: false);

            // Redirects to the return URL if login is successful
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }

            // Redirects to the 2FA page if two-factor authentication is required
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new
                {
                    ReturnUrl = returnUrl,
                    RememberMe = Input.RememberMe
                });
            }

            // Redirects to the lockout page if the account is locked
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }

            // Adds a generic login error if authentication fails
            ModelState.AddModelError(string.Empty, "Email veya şifre hatalı.");
            return Page();
        }
    }
}