using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Flipped_Classroom.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly Swp391NihongoContext _context;
        private readonly IAuthService _authService;

        public CreateModel(Swp391NihongoContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required(ErrorMessage = "Tên tài khoản là bắt buộc.")]
            [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên tài khoản phải từ 3 đến 50 ký tự.")]
            [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên tài khoản chỉ được chứa chữ cái, chữ số và dấu gạch dưới.")]
            [Display(Name = "Tên tài khoản")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email là bắt buộc.")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
            [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Tên (First Name) là bắt buộc.")]
            [StringLength(50, ErrorMessage = "Tên không được quá 50 ký tự.")]
            [Display(Name = "Tên")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Họ (Last Name) là bắt buộc.")]
            [StringLength(50, ErrorMessage = "Họ không được quá 50 ký tự.")]
            [Display(Name = "Họ")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vai trò là bắt buộc.")]
            [Display(Name = "Vai trò")]
            public string Role { get; set; } = "Student";

            [Display(Name = "Trạng thái kích hoạt")]
            public bool IsActive { get; set; } = true;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if Username is unique
            var usernameExists = await _context.Users.AnyAsync(u => u.Username == Input.Username.Trim());
            if (usernameExists)
            {
                ModelState.AddModelError("Input.Username", "Tên tài khoản này đã được sử dụng.");
            }

            // Check if Email is unique
            var emailExists = await _context.Users.AnyAsync(u => u.Email == Input.Email.Trim());
            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "Email này đã được sử dụng.");
            }

            if (usernameExists || emailExists)
            {
                return Page();
            }

            // Create and save User
            var user = new User
            {
                Username = Input.Username.Trim(),
                Email = Input.Email.Trim(),
                FirstName = Input.FirstName.Trim(),
                LastName = Input.LastName.Trim(),
                PasswordHash = _authService.HashPassword(Input.Password),
                Role = Input.Role,
                IsActive = Input.IsActive,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo thành công tài khoản {user.Username}.";

            return RedirectToPage("/Admin/Users/Index");
        }

    }
}
