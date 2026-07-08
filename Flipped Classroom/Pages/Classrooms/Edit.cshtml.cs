using System.Linq;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

            var classroom = await _context
                .Classes.Include(c => c.Manager)
                .FirstOrDefaultAsync(m => m.Id == id);
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
            ModelState.Remove("Class.Curriculum");

            if (!ModelState.IsValid)
            {
                if (Class != null && Class.ManagerId > 0)
                {
                    Class.Manager = await _context.Users.FindAsync(Class.ManagerId);
                }
                var managers = _context.Users.Where(u => u.Role == "Teacher").ToList();
                ViewData["ManagerId"] = new SelectList(managers, "Id", "Username");
                return Page();
            }

            // Kiểm tra trùng lịch giảng dạy nếu thay đổi giáo viên phụ trách lớp
            var originalClass = await _context
                .Classes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == Class.Id);
            if (originalClass != null && originalClass.ManagerId != Class.ManagerId)
            {
                var classSchedules = await _context
                    .ClassSchedules.Where(s => s.ClassId == Class.Id)
                    .ToListAsync();

                if (classSchedules.Any())
                {
                    var newTeacherSchedules = await _context
                        .ClassSchedules.Include(s => s.Class)
                        .Where(s => s.Class.ManagerId == Class.ManagerId && s.ClassId != Class.Id)
                        .ToListAsync();

                    foreach (var session in classSchedules)
                    {
                        var conflict = newTeacherSchedules.FirstOrDefault(es =>
                            es.StudyDate == session.StudyDate
                            && es.StartTime < session.EndTime
                            && es.EndTime > session.StartTime
                        );

                        if (conflict != null)
                        {
                            var newTeacher = await _context.Users.FindAsync(Class.ManagerId);
                            string teacherName =
                                newTeacher != null
                                    ? $"{newTeacher.FirstName} {newTeacher.LastName}"
                                    : "Giáo viên mới";

                            ModelState.AddModelError(
                                string.Empty,
                                $"Không thể đổi giáo viên. Giáo viên '{teacherName}' bị trùng lịch dạy ở lớp '{conflict.Class.ClassName}' "
                                    + $"vào ngày {session.StudyDate:dd/MM/yyyy} lúc {conflict.StartTime:HH:mm} - {conflict.EndTime:HH:mm}."
                            );

                            Class.Manager = newTeacher;
                            var managers = _context.Users.Where(u => u.Role == "Teacher").ToList();
                            ViewData["ManagerId"] = new SelectList(managers, "Id", "Username");
                            return Page();
                        }
                    }
                }
            }

            _context.Attach(Class).State = EntityState.Modified;

            _context.Entry(Class).Property(x => x.CreatedAt).IsModified = false;
            _context.Entry(Class).Property(x => x.InviteCode).IsModified = false;
            _context.Entry(Class).Property(x => x.CurriculumId).IsModified = false;

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
