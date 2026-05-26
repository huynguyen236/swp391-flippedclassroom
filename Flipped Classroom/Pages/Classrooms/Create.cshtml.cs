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
                           .Where(u => u.Role == "Manager" || u.Role == "Admin")
                           .ToList();

            // 2. Nạp danh sách đã lọc vào ViewData dưới dạng SelectList
            ViewData["ManagerId"] = new SelectList(managers, "Id", "Username");
            return Page();
        }

        [BindProperty]
        public Class Class { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || _context.Classes == null || Class == null)
            {
                ViewData["ManagerId"] = new SelectList(_context.Users, "Id", "Username");
                return Page();
            }

            Class.CreatedAt = DateTime.Now;
            Class.Status = "Active";

            _context.Classes.Add(Class);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}