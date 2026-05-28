using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace Flipped_Classroom.Pages.Classrooms
{
    [Authorize(Roles = "Admin,Manager")]
    public class EditModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;

        public EditModel(Flipped_Classroom.Data.Swp391NihongoContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Class Class { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Classes == null)
            {
                return NotFound();
            }

            var classroom = await _context.Classes.Include(c => c.Manager).FirstOrDefaultAsync(m => m.Id == id);
            if (classroom == null)
            {
                return NotFound();
            }
            Class = classroom;

            var managers = _context.Users.Where(u => u.Role == "Teacher").ToList();
            ViewData["ManagerId"] = new SelectList(managers, "Id", "Username");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Class.Manager");

            if (!ModelState.IsValid)
            {
                var managers = _context.Users.Where(u => u.Role == "Teacher").ToList();
                ViewData["ManagerId"] = new SelectList(managers, "Id", "Username");
                return Page();
            }

            _context.Attach(Class).State = EntityState.Modified;

            _context.Entry(Class).Property(x => x.CreatedAt).IsModified = false;
            _context.Entry(Class).Property(x => x.InviteCode).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClassExists(Class.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ClassExists(int id)
        {
          return (_context.Classes?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
