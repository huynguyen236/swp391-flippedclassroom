using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.Classrooms
{
    [Authorize(Roles = "Admin,Manager")]
    public class CreateModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;

        public CreateModel(Flipped_Classroom.Data.Swp391NihongoContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            var managers = _context.Users
                           .Where(u => u.Role == "Teacher")
                           .ToList();

            // 2. Nạp danh sách đã lọc vào ViewData dưới dạng SelectList
            ViewData["ManagerId"] = new SelectList(managers, "Id", "Username");
            return Page();
        }

        [BindProperty]
        public Class Class { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove navigation properties from validation
            ModelState.Remove("Class.Manager");

            if (!ModelState.IsValid || _context.Classes == null || Class == null)
            {
                var managers = _context.Users.Where(u => u.Role == "Teacher").ToList();
                ViewData["ManagerId"] = new SelectList(managers, "Id", "Username");
                return Page();
            }

            Class.CreatedAt = DateTime.Now;
            Class.Status = "Active";
            // Generate an 8-character uppercase alphanumeric invite code
            Class.InviteCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            _context.Classes.Add(Class);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}