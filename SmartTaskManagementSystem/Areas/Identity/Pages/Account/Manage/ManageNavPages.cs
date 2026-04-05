using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartTaskManagementSystem.Areas.Identity.Pages.Account.Manage
{
    // Helper class used to manage navigation state (active page highlighting)
    // within the Identity Manage section (Profile, Email, Password, etc.)
    public static class ManageNavPages
    {
        // Page identifiers (used for comparison with current active page)
        public static string Index => "Index";
        public static string Email => "Email";
        public static string ChangePassword => "ChangePassword";
        public static string TwoFactorAuthentication => "TwoFactorAuthentication";
        public static string PersonalData => "PersonalData";

        // Returns "active" CSS class if current page is Index
        public static string IndexNavClass(ViewContext viewContext) => PageNavClass(viewContext, Index);

        // Returns "active" CSS class if current page is Email
        public static string EmailNavClass(ViewContext viewContext) => PageNavClass(viewContext, Email);

        // Returns "active" CSS class if current page is ChangePassword
        public static string ChangePasswordNavClass(ViewContext viewContext) => PageNavClass(viewContext, ChangePassword);

        // Returns "active" CSS class if current page is TwoFactorAuthentication
        public static string TwoFactorAuthenticationNavClass(ViewContext viewContext) => PageNavClass(viewContext, TwoFactorAuthentication);

        // Returns "active" CSS class if current page is PersonalData
        public static string PersonalDataNavClass(ViewContext viewContext) => PageNavClass(viewContext, PersonalData);

        // Core method that determines whether a navigation item is active
        private static string PageNavClass(ViewContext viewContext, string page)
        {
            // Retrieves the current active page from ViewData
            var activePage = viewContext.ViewData["ActivePage"] as string;

            // Compares current page with target page (case-insensitive)
            // Returns "active" if matched, otherwise returns empty string
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : "";
        }
    }
}