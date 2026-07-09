using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Flipped_Classroom.Pages.Users
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(IUserService userService, IWebHostEnvironment environment, ILogger<ProfileModel> logger)
        {
            _userService = userService;
            _environment = environment;
            _logger = logger;
        }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public ProfileInputModel ProfileInput { get; set; } = new ProfileInputModel();

        [BindProperty]
        public PasswordInputModel PasswordInput { get; set; } = new PasswordInputModel();

        public User? CurrentUser { get; private set; }

        public class ProfileInputModel
        {
            [Required(ErrorMessage = "Tên không được để trống.")]
            [StringLength(50, ErrorMessage = "Tên không quá 50 ký tự.")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Họ không được để trống.")]
            [StringLength(50, ErrorMessage = "Họ không quá 50 ký tự.")]
            public string LastName { get; set; } = string.Empty;

            [RegularExpression(@"^(0[35789]\d{8})?$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 03, 05, 07, 08 hoặc 09 và gồm 10 chữ số.")]
            public string? PhoneNumber { get; set; }

            public IFormFile? AvatarFile { get; set; }
        }

        public class PasswordInputModel
        {
            [DataType(DataType.Password)]
            public string? CurrentPassword { get; set; }

            [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
            [MinLength(6, ErrorMessage = "Mật khẩu mới phải dài ít nhất 6 ký tự.")]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống.")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không trùng khớp.")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        private async Task LoadCurrentUserAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                CurrentUser = await _userService.GetUserByIdAsync(userId);
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCurrentUserAsync();
            if (CurrentUser == null)
            {
                return RedirectToPage("/Authentication/Login");
            }

            // Populate profile input
            ProfileInput.FirstName = CurrentUser.FirstName;
            ProfileInput.LastName = CurrentUser.LastName;
            ProfileInput.PhoneNumber = CurrentUser.PhoneNumber;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            await LoadCurrentUserAsync();
            if (CurrentUser == null)
            {
                return RedirectToPage("/Authentication/Login");
            }

            ModelState.Clear();
            if (!TryValidateModel(ProfileInput, nameof(ProfileInput)))
            {
                foreach (var modelStateKey in ModelState.Keys)
                 {
                    var modelStateVal = ModelState[modelStateKey];
                    foreach (var error in modelStateVal.Errors)
                    {
                        _logger.LogWarning("Validation error for {Key}: {Error}", modelStateKey, error.ErrorMessage);
                    }
                }
                return Page();
            }

            string? avatarUrl = CurrentUser.AvatarUrl;

            // Handle Avatar Upload
            if (ProfileInput.AvatarFile != null)
            {
                var file = ProfileInput.AvatarFile;

                // 1. Validate File Size (< 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProfileInput.AvatarFile", "Ảnh đại diện phải nhỏ hơn 5MB.");
                    return Page();
                }

                // 2. Validate File Extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ProfileInput.AvatarFile", "Chỉ hỗ trợ định dạng ảnh (.jpg, .jpeg, .png, .gif).");
                    return Page();
                }

                try
                {
                    // 3. Create upload path if not exists
                    var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    // 4. Generate unique filename
                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadDir, uniqueFileName);

                    // 5. Delete old file if exists and is local
                    if (!string.IsNullOrWhiteSpace(CurrentUser.AvatarUrl) && CurrentUser.AvatarUrl.StartsWith("/uploads/avatars/"))
                    {
                        var oldFilePath = Path.Combine(_environment.WebRootPath, CurrentUser.AvatarUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // 6. Save new file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    avatarUrl = $"/uploads/avatars/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    ErrorMessage = "Không thể tải lên ảnh đại diện: " + ex.Message;
                    return Page();
                }
            }

            var (success, errorMsg) = await _userService.UpdateProfileAsync(
                CurrentUser.Id, 
                ProfileInput.FirstName, 
                ProfileInput.LastName, 
                ProfileInput.PhoneNumber, 
                avatarUrl);

            if (!success)
            {
                ErrorMessage = errorMsg;
                return Page();
            }

            // Update Cookie Claims to immediately reflect name changes in layout header/sidebar
            var claims = User.Claims.ToList();
            
            // Remove old FullName claim if exists
            var fullNameClaim = claims.FirstOrDefault(c => c.Type == "FullName");
            if (fullNameClaim != null) claims.Remove(fullNameClaim);
            
            // Add new FullName claim
            claims.Add(new Claim("FullName", $"{ProfileInput.FirstName} {ProfileInput.LastName}"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                });

            SuccessMessage = "Cập nhật thông tin cá nhân thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            await LoadCurrentUserAsync();
            if (CurrentUser == null)
            {
                return RedirectToPage("/Authentication/Login");
            }

            // Clear profile input errors
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("ProfileInput")).ToList())
            {
                ModelState.Remove(key);
            }

            // If user doesn't have a password hash (e.g. Google Sign-In only), CurrentPassword is not required
            if (CurrentUser.PasswordHash == null)
            {
                ModelState.Remove("PasswordInput.CurrentPassword");
            }
            else if (string.IsNullOrWhiteSpace(PasswordInput.CurrentPassword))
            {
                ModelState.AddModelError("PasswordInput.CurrentPassword", "Mật khẩu hiện tại là bắt buộc.");
            }

            if (!ModelState.IsValid)
            {
                // Lỗi validation (mật khẩu trống, không khớp...) -> hiển thị ngay dưới ô nhập.
                // Đồng thời đưa ra thông báo lỗi tổng quát ở đầu trang.
                ErrorMessage = "Vui lòng kiểm tra lại thông tin đổi mật khẩu.";

                // Restore profile input to display correctly on return
                ProfileInput.FirstName = CurrentUser.FirstName;
                ProfileInput.LastName = CurrentUser.LastName;
                ProfileInput.PhoneNumber = CurrentUser.PhoneNumber;
                return Page();
            }

            var (success, errorMsg) = await _userService.ChangePasswordAsync(
                CurrentUser.Id,
                PasswordInput.CurrentPassword ?? string.Empty,
                PasswordInput.NewPassword);

            if (!success)
            {
                // Ví dụ: nhập sai mật khẩu hiện tại -> báo lỗi rõ ràng cho người dùng.
                ErrorMessage = errorMsg ?? "Đổi mật khẩu thất bại. Vui lòng thử lại.";

                // Redirect (PRG) để TempData luôn hiển thị được thông báo và tránh gửi lại form.
                return RedirectToPage();
            }

            SuccessMessage = "Thay đổi mật khẩu thành công.";
            return RedirectToPage();
        }
    }
}
