using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.TeacherClasses
{
    [Authorize(Roles = "Teacher")]
    public class DetailsModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;
        private readonly ILessonService _lessonService;
        private readonly IQuizService _quizService;

        public DetailsModel(Flipped_Classroom.Data.Swp391NihongoContext context, ILessonService lessonService, IQuizService quizService)
        {
            _context = context;
            _lessonService = lessonService;
            _quizService = quizService;
        }

        public Class Class { get; set; } = default!;

        public List<User> AvailableUsers { get; set; } = default!;

        // Trạng thái mở/khóa từng node trong lớp (nodeId -> đã mở chưa). Không có khóa = chưa mở.
        public Dictionary<int, bool> NodeUnlockStatus { get; set; } = new();

        // Số học sinh đã hoàn thành mỗi node (nodeId -> số HS hoàn thành)
        public Dictionary<int, int> NodeCompletionCounts { get; set; } = new();

        // Tổng số học sinh trong lớp (mẫu số "x/y HS hoàn thành")
        public int TotalStudents { get; set; }

        [BindProperty]
        public List<int> SelectedStudentIds { get; set; } = new List<int>();

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
                .Include(c => c.Curriculum)
                    .ThenInclude(cu => cu!.Nodes)
                        .ThenInclude(n => n.Materials)
                .Include(c => c.Curriculum)
                    .ThenInclude(cu => cu!.Nodes)
                        .ThenInclude(n => n.Quizzes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
            {
                return NotFound();
            }

            // Verify teacher is the manager
            if (classroom.ManagerId != userId)
            {
                return Forbid();
            }

            Class = classroom;

            // Get available users who are not currently in the classroom
            var existingMemberIds = classroom.ClassMembers.Select(cm => cm.UserId).ToList();
            var availableUsers = await _context.Users
                .Where(u => !existingMemberIds.Contains(u.Id) && u.Role == "Student")
                .OrderBy(u => u.Username)
                .ToListAsync();

            AvailableUsers = availableUsers;
            NumberOfGroupsToCreate = classroom.Groups?.Count ?? 0;

            // Trạng thái mở/khóa node của lớp này
            NodeUnlockStatus = await _lessonService.GetNodeUnlockStatusAsync(classroom.Id);

            // Tổng số học sinh và số HS hoàn thành mỗi node
            TotalStudents = classroom.ClassMembers?.Count ?? 0;
            NodeCompletionCounts = await _context.StudentProgresses
                .AsNoTracking()
                .Where(p => p.ClassId == classroom.Id && p.IsCompleted == true)
                .GroupBy(p => p.NodeId)
                .Select(g => new { NodeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.NodeId, x => x.Count);

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
                var memberExists = await _context.ClassMembers.AnyAsync(cm => cm.ClassId == id && cm.UserId == studentId);
                if (!memberExists)
                {
                    var newMember = new ClassMember
                    {
                        ClassId = id,
                        UserId = studentId,
                        JoinedAt = DateTime.Now,
                        IsSupportTeam = false
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
            if (NumberOfGroupsToCreate <= 0) return RedirectToPage(new { id });

            var classExists = await _context.Classes.Include(c => c.Groups).FirstOrDefaultAsync(c => c.Id == id);
            if (classExists == null) return NotFound();

            int groupsToAdd = NumberOfGroupsToCreate - classExists.Groups.Count;

            if (groupsToAdd > 0)
            {
                for (int i = 1; i <= groupsToAdd; i++)
                {
                    var newGroup = new Group
                    {
                        ClassId = id,
                        GroupName = "temp", // Name will be updated below
                        CreatedAt = DateTime.Now
                    };
                    classExists.Groups.Add(newGroup);
                    _context.Groups.Add(newGroup);
                }
            }
            else if (groupsToAdd < 0)
            {
                var groupsToRemove = classExists.Groups
                    .OrderByDescending(g => g.Id)
                    .Take(Math.Abs(groupsToAdd))
                    .ToList();

                foreach (var g in groupsToRemove)
                {
                    classExists.Groups.Remove(g);
                }
                _context.Groups.RemoveRange(groupsToRemove);
            }

            // Standardize all group names sequentially (Nh�m 1, Nh�m 2, ...)
            var allGroups = classExists.Groups.OrderBy(g => g.Id == 0 ? int.MaxValue : g.Id).ToList();
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
            var classGroupIds = await _context.Groups.Where(g => g.ClassId == id).Select(g => g.Id).ToListAsync();
            var existingMemberships = await _context.GroupMembers
                .Where(gm => classGroupIds.Contains(gm.GroupId) && gm.StudentId == studentId)
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
                    StudentId = studentId
                };
                _context.GroupMembers.Add(newMembership);
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRemoveStudentAsync(int id, int memberUserId)
        {
            var classMember = await _context.ClassMembers
                .FirstOrDefaultAsync(cm => cm.ClassId == id && cm.UserId == memberUserId);

            if (classMember != null)
            {
                _context.ClassMembers.Remove(classMember);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id = id });
        }

        // Mở / khóa một node cho lớp này. Chỉ giáo viên quản lý lớp mới được thao tác.
        // Trả về JSON để JS cập nhật giao diện mà không reload trang.
        public async Task<IActionResult> OnPostToggleNodeLockAsync(int id, int nodeId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Phiên đăng nhập đã hết hạn." }) { StatusCode = 401 };
            }

            var classroom = await _context.Classes.FirstOrDefaultAsync(c => c.Id == id);
            if (classroom == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy lớp học." }) { StatusCode = 404 };
            }

            if (classroom.ManagerId != userId)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền thao tác lớp này." }) { StatusCode = 403 };
            }

            await _lessonService.ToggleNodeLockAsync(id, nodeId);
            var isUnlocked = await _lessonService.IsNodeUnlockedAsync(id, nodeId);

            return new JsonResult(new { success = true, isUnlocked });
        }

        public async Task<IActionResult> OnPostDeleteQuizAsync(int id, int quizId)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz != null)
            {
                _context.Quizzes.Remove(quiz);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa bài test thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài test để xóa.";
            }
            return RedirectToPage(new { id });
        }
    }
}
