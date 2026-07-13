using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.Classrooms
{
    [Authorize(Roles = "Admin,Manager")]
    public class IndexModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;
        private readonly IAuthService _authService;

        public IndexModel(
            Flipped_Classroom.Data.Swp391NihongoContext context,
            IAuthService authService
        )
        {
            _context = context;
            _authService = authService;
        }

        public IList<Class> Class { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public async Task OnGetAsync()
        {
            await _context.AutoInactivateExpiredClassesAsync();

            if (_context.Classes != null)
            {
                var classesQuery = _context.Classes.Include(c => c.Manager).AsQueryable();

                if (!string.IsNullOrEmpty(SearchString))
                {
                    classesQuery = classesQuery.Where(c => c.ClassName.Contains(SearchString));
                }

                // Pagination
                int pageSize = 6;
                var count = await classesQuery.CountAsync();
                TotalPages = (int)System.Math.Ceiling(count / (double)pageSize);

                // Ensure PageIndex is within boundaries
                if (PageIndex < 1)
                    PageIndex = 1;
                if (PageIndex > TotalPages && TotalPages > 0)
                    PageIndex = TotalPages;

                Class = await classesQuery
                    .Skip((PageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnGetClassMembersAsync(int classId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Unauthorized" })
                {
                    StatusCode = 401,
                };
            }

            var cls = await _context
                .Classes.Include(c => c.ClassMembers)
                    .ThenInclude(cm => cm.User)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (cls == null)
            {
                return new JsonResult(new { success = false, message = "Class not found" })
                {
                    StatusCode = 404,
                };
            }

            if (cls.ManagerId != userId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                return new JsonResult(new { success = false, message = "Forbidden" })
                {
                    StatusCode = 403,
                };
            }

            var members = cls
                .ClassMembers.Where(cm => cm.User != null)
                .Select(cm => new
                {
                    id = cm.User.Id,
                    username = cm.User.Username,
                    email = cm.User.Email,
                })
                .ToList();

            return new JsonResult(members);
        }

        public async Task<IActionResult> OnPostSendNotificationAsync(
            int classId,
            List<int> selectedStudentIds,
            string subject,
            string body
        )
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Unauthorized" })
                {
                    StatusCode = 401,
                };
            }

            var cls = await _context
                .Classes.Include(c => c.ClassMembers)
                    .ThenInclude(cm => cm.User)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (cls == null)
            {
                return new JsonResult(new { success = false, message = "Class not found" })
                {
                    StatusCode = 404,
                };
            }

            if (cls.ManagerId != userId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                return new JsonResult(new { success = false, message = "Forbidden" })
                {
                    StatusCode = 403,
                };
            }

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                return new JsonResult(
                    new { success = false, message = "Subject and body cannot be empty" }
                )
                {
                    StatusCode = 400,
                };
            }

            var students = cls
                .ClassMembers.Where(cm => cm.User != null)
                .Select(cm => cm.User)
                .ToList();

            if (selectedStudentIds != null && selectedStudentIds.Any())
            {
                students = students.Where(s => selectedStudentIds.Contains(s.Id)).ToList();
            }

            if (!students.Any())
            {
                return new JsonResult(
                    new
                    {
                        success = false,
                        message = "No students selected or found in this class.",
                    }
                )
                {
                    StatusCode = 400,
                };
            }

            foreach (var student in students)
            {
                if (!string.IsNullOrWhiteSpace(student.Email))
                {
                    var email = student.Email;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _authService.SendEmailAsync(email, subject, body);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending email to {email}: {ex.Message}");
                        }
                    });
                }
            }

            return new JsonResult(
                new { success = true, message = "Thông báo email đã được đưa vào hàng đợi gửi đi." }
            );
        }
    }
}
