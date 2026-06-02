using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.MyClasses
{
    [Authorize(Roles = "Student")]
    public class DetailsModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public DetailsModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public Class Class { get; set; } = default!; 

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Classes == null)
            {
                return NotFound();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            var classroom = await _context.Classes
                .Include(c => c.Manager)
                .Include(c => c.ClassMembers)
                    .ThenInclude(cm => cm.User)
                .Include(c => c.Groups)
                    .ThenInclude(g => g.GroupMembers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
            {
                return NotFound();
            }

            // Verify the student is actually in this class
            if (!classroom.ClassMembers.Any(cm => cm.UserId == userId))
            {
                return Forbid();
            }

            Class = classroom;

            return Page();
        }
    }
}
