using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartTaskManagementSystem.Areas.Identity.Pages.Account
{
    // Handles user registration operations for the Identity register page
    public class RegisterModel : PageModel
    {
        // Manages sign-in related operations
        private readonly SignInManager<IdentityUser> _signInManager;

        // Manages user creation and user-related operations
        private readonly UserManager<IdentityUser> _userManager;

        // Provides access to the user store
        private readonly IUserStore<IdentityUser> _userStore;

        // Logs registration and system events
        private readonly ILogger<RegisterModel> _logger;

        // Constructor for dependency injection
        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _logger = logger;
        }

        // Binds registration form input values to the model
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Stores the return URL after successful registration
        public string ReturnUrl { get; set; } = string.Empty;

        // Stores available external authentication providers
        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        // Represents the user input fields on the registration form
        public class InputModel
        {
            // User email address
            [Required(ErrorMessage = "Email zorunludur.")]
            [EmailAddress(ErrorMessage = "Geçerli bir email giriniz.")]
            public string Email { get; set; } = string.Empty;

            // User password
            [Required(ErrorMessage = "Şifre zorunludur.")]
            [StringLength(100, ErrorMessage = "Şifre en az 6 karakter olmalı.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            // Password confirmation field
            [Required(ErrorMessage = "Şifre tekrar zorunludur.")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // Handles GET requests for the registration page
        public async Task OnGetAsync(string? returnUrl = null)
        {
            // Sets the return URL or defaults to the home page
            ReturnUrl = returnUrl ?? Url.Content("~/");

            // Loads configured external login providers
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        // Handles POST requests when the registration form is submitted
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

            // Creates a new Identity user using the provided email address
            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email
            };

            // Attempts to create the user with the provided password
            var result = await _userManager.CreateAsync(user, Input.Password);

            // Signs in the user and redirects if registration is successful
            if (result.Succeeded)
            {
                _logger.LogInformation("Yeni kullanıcı oluşturuldu.");

                await _signInManager.SignInAsync(user, isPersistent: false);

                return LocalRedirect(returnUrl);
            }

            // Adds Identity errors to the model state if registration fails
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}