using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.Authentication
{
    /// <summary>
    /// Không render UI. OnGet() redirect user sang Google consent screen.
    /// </summary>
    public class GoogleLoginModel : PageModel
    {
        public IActionResult OnGet(string? returnUrl = null)
        {
            var redirectUri = Url.Page(
                "/Authentication/GoogleCallback",
                pageHandler: null,
                values: new { returnUrl },
                protocol: Request.Scheme);

            // If redirect URI cannot be built, fall back to login page
            if (string.IsNullOrEmpty(redirectUri))
            {
                TempData["ErrorMessage"] = "Unable to initiate Google sign-in. Please try again.";
                return RedirectToPage("/Authentication/Login");
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

            // Dùng string "Google" thay vì GoogleDefaults.AuthenticationScheme
            // — hoàn toàn tương đương, không cần using Google assembly
            return Challenge(properties, "Google");
        }
    }
}
