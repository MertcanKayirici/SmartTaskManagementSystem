using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartTaskManagementSystem.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Handles user logout operations within the Identity area.
    /// Responsible for terminating the authentication session.
    /// </summary>
    public class LogoutModel : PageModel
    {
        /// <summary>
        /// ASP.NET Core Identity manager used to handle sign-in and sign-out operations.
        /// </summary>
        private readonly SignInManager<IdentityUser> _signInManager;

        /// <summary>
        /// Initializes the page model with required dependencies.
        /// </summary>
        /// <param name="signInManager">Identity sign-in manager.</param>
        public LogoutModel(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        /// <summary>
        /// Handles POST request to log out the current user.
        /// </summary>
        /// <param name="returnUrl">Optional URL to redirect after logout.</param>
        /// <returns>Redirect result to specified page or default home page.</returns>
        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // Sign out the currently authenticated user
            await _signInManager.SignOutAsync();

            // Redirect to provided return URL if available
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }

            // Fallback redirect to application home page
            return RedirectToPage("Home/Index");
        }
    }
}