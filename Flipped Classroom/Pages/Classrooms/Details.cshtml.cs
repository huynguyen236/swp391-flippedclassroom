using System;
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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.Classrooms
{
    [Authorize(Roles = "Admin,Manager")]
    public class DetailsModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;
        private readonly IAuthService _authService;

        public DetailsModel(
            Flipped_Classroom.Data.Swp391NihongoContext context,
            IAuthService authService
        )
        {
            _context = context;
            _authService = authService;
        }

        public Class Class { get; set; } = default!;

        public List<User> AvailableUsers { get; set; } = default!;

        [BindProperty]
        public List<int> SelectedStudentIds { get; set; } = new List<int>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Classes == null)
            {
                return NotFound();
            }

            var classroom = await _context
                .Classes.Include(c => c.Manager)
                .Include(c => c.ClassMembers)
                    .ThenInclude(cm => cm.User)
                .Include(c => c.Groups)
                    .ThenInclude(g => g.GroupMembers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
            {
                return NotFound();
            }

            Class = classroom;

            // Get available users who are not currently in the classroom
            var existingMemberIds = classroom.ClassMembers.Select(cm => cm.UserId).ToList();
            var availableUsers = await _context
                .Users.Where(u => !existingMemberIds.Contains(u.Id) && u.Role == "Student")
                .OrderBy(u => u.Username)
                .ToListAsync();

            AvailableUsers = availableUsers;
            NumberOfGroupsToCreate = classroom.Groups?.Count ?? 0;

            return Page();
        }

        public async Task<IActionResult> OnPostAddStudentsAsync(int id)
        {
            if (SelectedStudentIds == null || !SelectedStudentIds.Any())
            {
                return RedirectToPage(new { id = id });
            }

            var classExists = await _context.Classes.AnyAsync(c => c.Id == id);
            if (!classExists)
            {
                return NotFound();
            }

            foreach (var studentId in SelectedStudentIds)
            {
                var memberExists = await _context.ClassMembers.AnyAsync(cm =>
                    cm.ClassId == id && cm.UserId == studentId
                );
                if (!memberExists)
                {
                    var newMember = new ClassMember
                    {
                        ClassId = id,
                        UserId = studentId,
                        JoinedAt = DateTime.Now,
                        IsSupportTeam = false,
                    };
                    _context.ClassMembers.Add(newMember);
                }
            }
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = id });
        }

        [BindProperty]
        public int NumberOfGroupsToCreate { get; set; }

        public async Task<IActionResult> OnPostCreateGroupsAsync(int id)
        {
            if (NumberOfGroupsToCreate <= 0)
                return RedirectToPage(new { id });

            var classExists = await _context
                .Classes.Include(c => c.Groups)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (classExists == null)
                return NotFound();

            int groupsToAdd = NumberOfGroupsToCreate - classExists.Groups.Count;

            if (groupsToAdd > 0)
            {
                for (int i = 1; i <= groupsToAdd; i++)
                {
                    var newGroup = new Group
                    {
                        ClassId = id,
                        GroupName = "temp", // Name will be updated below
                        CreatedAt = DateTime.Now,
                    };
                    classExists.Groups.Add(newGroup);
                    _context.Groups.Add(newGroup);
                }
            }
            else if (groupsToAdd < 0)
            {
                var groupsToRemove = classExists
                    .Groups.OrderByDescending(g => g.Id)
                    .Take(Math.Abs(groupsToAdd))
                    .ToList();

                foreach (var g in groupsToRemove)
                {
                    classExists.Groups.Remove(g);
                }
                _context.Groups.RemoveRange(groupsToRemove);
            }

            // Standardize all group names sequentially (Nh�m 1, Nh�m 2, ...)
            var allGroups = classExists
                .Groups.OrderBy(g => g.Id == 0 ? int.MaxValue : g.Id)
                .ToList();
            for (int i = 0; i < allGroups.Count; i++)
            {
                allGroups[i].GroupName = $"Nh�m {i + 1}";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAssignGroupAsync(int id, int studentId, int? groupId)
        {
            // Remove existing group assignment for this student in this class
            var classGroupIds = await _context
                .Groups.Where(g => g.ClassId == id)
                .Select(g => g.Id)
                .ToListAsync();
            var existingMemberships = await _context
                .GroupMembers.Where(gm =>
                    classGroupIds.Contains(gm.GroupId) && gm.StudentId == studentId
                )
                .ToListAsync();

            if (existingMemberships.Any())
            {
                _context.GroupMembers.RemoveRange(existingMemberships);
            }

            if (groupId.HasValue)
            {
                var newMembership = new GroupMember
                {
                    GroupId = groupId.Value,
                    StudentId = studentId,
                };
                _context.GroupMembers.Add(newMembership);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRemoveStudentAsync(int id, int memberUserId)
        {
            var classMember = await _context.ClassMembers.FirstOrDefaultAsync(cm =>
                cm.ClassId == id && cm.UserId == memberUserId
            );

            if (classMember != null)
            {
                _context.ClassMembers.Remove(classMember);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id = id });
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
