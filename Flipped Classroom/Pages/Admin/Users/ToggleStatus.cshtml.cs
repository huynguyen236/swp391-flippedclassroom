using Flipped_Classroom.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Flipped_Classroom.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class ToggleStatusModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public ToggleStatusModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync(int id, string? searchTerm, string? roleFilter, string? activeFilter, int pageIndex)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Toggle active status (null treats as inactive, so toggles to active)
                user.IsActive = !(user.IsActive ?? false);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái tài khoản {user.Username}.";
            }

            // Redirect back preserving current filters and pagination state
            return RedirectToPage("/Admin/Users/Index", new
            {
                searchTerm = searchTerm,
                roleFilter = roleFilter,
                activeFilter = activeFilter,
                pageIndex = pageIndex
            });
        }
    }
}
