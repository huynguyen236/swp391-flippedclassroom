using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;
using Flipped_Classroom.Services.Interfaces;

namespace Flipped_Classroom.Pages.Classrooms
{
    [Authorize(Roles = "Admin,Manager")]
    public class CreateModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;
        private readonly IQuizService _quizService;

        public CreateModel(Flipped_Classroom.Data.Swp391NihongoContext context, IQuizService quizService)
        {
            _context = context;
            _quizService = quizService;
        }

        public IActionResult OnGet()
        {
            LoadSelectLists();
            return Page();
        }

        [BindProperty]
        public Class Class { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove navigation properties from validation
            ModelState.Remove("Class.Manager");
            ModelState.Remove("Class.Curriculum");

            // Bắt buộc chọn khung chương trình khi tạo lớp
            if (Class == null || Class.CurriculumId == null)
            {
                ModelState.AddModelError("Class.CurriculumId", "Vui lòng chọn khung chương trình cho lớp.");
            }

            if (!ModelState.IsValid || _context.Classes == null || Class == null)
            {
                LoadSelectLists();
                return Page();
            }

            Class.CreatedAt = DateTime.Now;
            Class.Status = "Active";
            // Generate an 8-character uppercase alphanumeric invite code
            Class.InviteCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            _context.Classes.Add(Class);
            await _context.SaveChangesAsync();

            // Clone template quizzes from Curriculum to the newly created Class
            if (Class.CurriculumId > 0)
            {
                await _quizService.CloneCurriculumQuizzesToClassAsync(Class.CurriculumId, Class.Id);
            }

            return RedirectToPage("./Index");
        }

        private void LoadSelectLists()
        {
            var managers = _context.Users
                .Where(u => u.Role == "Teacher")
                .ToList();
            ViewData["ManagerId"] = new SelectList(managers, "Id", "Username");

            var curriculums = _context.Curriculums
                .OrderBy(c => c.CurriculumName)
                .ToList();
            ViewData["CurriculumId"] = new SelectList(curriculums, "Id", "CurriculumName");
        }
    }
}