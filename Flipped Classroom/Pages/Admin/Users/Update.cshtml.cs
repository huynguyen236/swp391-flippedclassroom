using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Flipped_Classroom.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public EditModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            public int Id { get; set; }

            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email là bắt buộc.")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
            [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Tên (First Name) là bắt buộc.")]
            [StringLength(50, ErrorMessage = "Tên không được quá 50 ký tự.")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Họ (Last Name) là bắt buộc.")]
            [StringLength(50, ErrorMessage = "Họ không được quá 50 ký tự.")]
            public string LastName { get; set; } = string.Empty;

            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên.")]
            [DataType(DataType.Password)]
            public string? Password { get; set; }

            [Required(ErrorMessage = "Vai trò là bắt buộc.")]
            [Display(Name = "Vai trò")]
            public string Role { get; set; } = "Student";

            [Display(Name = "Trạng thái")]
            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive ?? false
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _context.Users.FindAsync(Input.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if Email is unique (excluding current user)
            var emailExists = await _context.Users.AnyAsync(u => u.Email == Input.Email.Trim() && u.Id != Input.Id);
            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "Email này đã được sử dụng bởi tài khoản khác.");
                return Page();
            }

            // Update details
            user.Email = Input.Email.Trim();
            user.FirstName = Input.FirstName.Trim();
            user.LastName = Input.LastName.Trim();
            user.Role = Input.Role;
            user.IsActive = Input.IsActive;

            // Update password if a new one is typed
            if (!string.IsNullOrWhiteSpace(Input.Password))
            {
                user.PasswordHash = Input.Password; // Plain text dynamic matching your rule
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật thành công tài khoản {user.Username}.";

            return RedirectToPage("/Admin/Users/Index");
        }
    }
}