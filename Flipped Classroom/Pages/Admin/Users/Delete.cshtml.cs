using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Flipped_Classroom.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public DeleteModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User UserToDelete { get; set; } = new User();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting an Admin account for security
            if (user.Role == "Admin")
            {
                return Forbid();
            }

            UserToDelete = user;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _context.Users.FindAsync(UserToDelete.Id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Role == "Admin")
            {
                return Forbid();
            }

            string savedUsername = user.Username;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn thành công tài khoản {savedUsername}.";

            return RedirectToPage("/Admin/Users/Index");
        }
    }
}