using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace Flipped_Classroom.Pages.Authentication
{
    /// <summary>
    /// Google redirect về đây SAU KHI middleware /signin-google đã xử lý token.
    /// Middleware populate HttpContext.User với claims từ Google rồi redirect về đây.
    /// Ta chỉ cần đọc HttpContext.User trực tiếp — không cần AuthenticateAsync.
    /// </summary>
    public class GoogleCallbackModel : PageModel
    {
        private readonly Swp391NihongoContext _context;
        private readonly ILogger<GoogleCallbackModel> _logger;

        public GoogleCallbackModel(Swp391NihongoContext context, ILogger<GoogleCallbackModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null, string? remoteError = null)
        {
            // Case 1: Google trả về lỗi tường minh — được xử lý bởi OnRemoteFailure
            // trong Program.cs trước khi vào đây, nhưng giữ lại để an toàn
            if (remoteError != null)
            {
                _logger.LogWarning("Google OAuth remoteError: {Error}", remoteError);
                TempData["ErrorMessage"] = "Google sign-in was cancelled. Please try again.";
                return RedirectToPage("/Authentication/Login");
            }

            // Case 2: Đọc claims trực tiếp từ HttpContext.User
            // Middleware /signin-google đã populate principal vào đây sau khi xác thực xong
            var principal = HttpContext.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("GoogleCallback: HttpContext.User not authenticated.");
                TempData["ErrorMessage"] = "Google sign-in failed. Please try again.";
                return RedirectToPage("/Authentication/Login");
            }

            var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(ClaimTypes.Email);
            var fullName = principal.FindFirstValue(ClaimTypes.Name) ?? email ?? "Unknown";
            var (firstName, lastName) = SplitName(fullName);

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("GoogleCallback: Missing required claims (googleId/email).");
                TempData["ErrorMessage"] = "Google did not provide required information. Please try again.";
                return RedirectToPage("/Authentication/Login");
            }

            // ── Nghiệp vụ: Tìm hoặc tạo User ────────────────────────────────────

            // 1. Đã từng login Google → tìm theo GoogleId
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (user == null)
            {
                // 2. Có account thường với email này → link GoogleId vào
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    user.GoogleId = googleId;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Linked Google to existing user: {Email}", email);
                }
                else
                {
                    // 3. User mới → tạo account, Password = null (passwordless)
                    user = new User
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Username = await GenerateUniqueUsernameAsync(email),
                        PasswordHash = null,
                        Email = email,
                        GoogleId = googleId,
                        Role = "Student",
                        CreatedAt = DateTime.Now
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new user via Google: {Email}", email);
                }
            }

            // ── Đăng nhập bằng Cookie scheme chính ───────────────────────────────
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,           user.Username),
                new Claim("FullName",                GetFullName(user)),
                new Claim(ClaimTypes.Role,           user.Role),
                new Claim(ClaimTypes.Email,          email)
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

            _logger.LogInformation("User {Username} signed in via Google.", user.Username);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToPage("/Index");
        }

        private async Task<string> GenerateUniqueUsernameAsync(string email)
        {
            var baseUsername = email.Split('@')[0]
                .ToLower()
                .Replace(".", "_")
                .Replace("-", "_");

            if (baseUsername.Length > 50)
                baseUsername = baseUsername[..50];

            var username = baseUsername;
            var counter = 2;

            while (await _context.Users.AnyAsync(u => u.Username == username))
                username = $"{baseUsername}_{counter++}";

            return username;
        }

        private static (string FirstName, string LastName) SplitName(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return ("Unknown", "User");
            }

            if (parts.Length == 1)
            {
                return (parts[0], "User");
            }

            return (parts[0], string.Join(" ", parts.Skip(1)));
        }

        private static string GetFullName(User user)
        {
            return string.Join(" ", new[] { user.FirstName, user.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
