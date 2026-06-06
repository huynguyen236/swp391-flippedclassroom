using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flipped_Classroom.Pages.MyClasses
{
    [Authorize(Roles = "Student")]
    public class IndexModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public IndexModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public IList<Class> Class { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                if (_context.Classes != null)
                {
                    Class = await _context.Classes
                        .Include(c => c.Manager)
                        .Where(c => c.ClassMembers.Any(cm => cm.UserId == userId))
                        .ToListAsync();
                }
                return Page();
            }

            return RedirectToPage("/Authentication/Login");
        }
    }
}
