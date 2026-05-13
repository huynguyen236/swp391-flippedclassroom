using Flipped_Classroom.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Flipped_Classroom.Pages.Authentication
{
    public class LoginModel : PageModel
    {
        private readonly FlippedClassroomContext _context;

        public LoginModel(FlippedClassroomContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Username is required.")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null, bool cancelled = false)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToPage("/Index");

            // User bấm Cancel ở Google consent screen
            if (cancelled)
                TempData["ErrorMessage"] = "Google sign-in was cancelled. Please try again.";

            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return Page();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == Input.Username);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // Tài khoản Google (Password = null) → không cho login bằng form
            if (user.Password == null)
            {
                ModelState.AddModelError(string.Empty,
                    "This account uses Google Sign-In. Please use the \"Continue with Google\" button.");
                return Page();
            }

            if (user.Password != Input.Password)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name,           user.Username),
                new Claim("FullName",                user.FullName),
                new Claim(ClaimTypes.Role,           user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProps);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToPage("/Index");
        }
    }
}
