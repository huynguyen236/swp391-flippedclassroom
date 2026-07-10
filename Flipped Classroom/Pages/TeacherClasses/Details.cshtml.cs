using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.TeacherClasses
{
    [Authorize(Roles = "Teacher")]
    public class DetailsModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;
        private readonly IAssignmentService _assignmentService;
        private readonly ISubmissionService _submissionService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILessonService _lessonService;
        private readonly IAuthService _authService;

        public DetailsModel(
            Flipped_Classroom.Data.Swp391NihongoContext context,
            IAssignmentService assignmentService,
            ISubmissionService submissionService,
            IWebHostEnvironment webHostEnvironment,
            ILessonService lessonService,
            IAuthService authService
        )
        {
            _context = context;
            _assignmentService = assignmentService;
            _submissionService = submissionService;
            _webHostEnvironment = webHostEnvironment;
            _lessonService = lessonService;
            _authService = authService;
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

        [BindProperty]
        [Range(1, 6, ErrorMessage = "Số lượng nhóm phải từ 1 đến 6.")]
        public int NumberOfGroupsToCreate { get; set; }

        public List<Assignment> Assignments { get; set; } = new();
        public Dictionary<int, List<Submission>> AssignmentSubmissions { get; set; } = new();

        [BindProperty]
        public CreateAssignmentRequest NewAssignment { get; set; } = default!;

        [BindProperty]
        public GradeSubmissionRequest GradeRequest { get; set; } = default!;

        public List<QaThread> QaThreads { get; set; } = new();

        // Tiến độ của từng học sinh: StudentId -> (NodeId -> CompletedAt)
        public Dictionary<int, Dictionary<int, DateTime?>> StudentProgressMap { get; set; } = new();

        public int TotalLessonsCount { get; set; }
        public double AvgClassProgress { get; set; }
        public double AvgSubmissionRate { get; set; }

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

            var success = await LoadTeacherClassroomDataAsync(id.Value, userId);
            if (!success)
            {
                var classroomExists = await _context.Classes.AnyAsync(c => c.Id == id);
                if (classroomExists)
                {
                    return Forbid();
                }
                return NotFound();
            }

            // Trạng thái mở/khóa node của lớp này
            NodeUnlockStatus = await _lessonService.GetNodeUnlockStatusAsync(Class.Id);

            // Tổng số học sinh và số HS hoàn thành mỗi node
            TotalStudents = Class.ClassMembers?.Count ?? 0;
            NodeCompletionCounts = await _context
                .StudentProgresses.AsNoTracking()
                .Where(p => p.ClassId == Class.Id && p.IsCompleted == true)
                .GroupBy(p => p.NodeId)
                .Select(g => new { NodeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.NodeId, x => x.Count);

            // Tiến độ chi tiết từng học sinh trong lớp
            var progressList = await _context
                .StudentProgresses.AsNoTracking()
                .Where(p => p.ClassId == Class.Id && p.IsCompleted == true)
                .Select(p => new
                {
                    p.StudentId,
                    p.NodeId,
                    p.CompletedAt,
                })
                .ToListAsync();

            StudentProgressMap = progressList
                .GroupBy(p => p.StudentId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.NodeId, x => x.CompletedAt));

            // Tính toán thống kê cho dashboard
            if (Class.Curriculum != null && Class.Curriculum.Nodes != null)
            {
                var lessonNodes = Class
                    .Curriculum.Nodes.Where(n => n.ParentNodeId != null)
                    .ToList();
                TotalLessonsCount = lessonNodes.Count;

                if (TotalStudents > 0 && TotalLessonsCount > 0)
                {
                    var totalPossibleCompletions = TotalStudents * TotalLessonsCount;
                    var completedLessonNodeIds = lessonNodes.Select(n => n.Id).ToList();
                    var actualCompletions = progressList.Count(p =>
                        completedLessonNodeIds.Contains(p.NodeId)
                    );
                    AvgClassProgress = Math.Round(
                        (double)actualCompletions * 100 / totalPossibleCompletions,
                        1
                    );
                }
            }

            if (Assignments != null && Assignments.Any() && TotalStudents > 0)
            {
                var totalPossibleSubmissions = Assignments.Count * TotalStudents;
                var totalActualSubmissions = AssignmentSubmissions.Values.Sum(list => list.Count);
                AvgSubmissionRate = Math.Round(
                    (double)totalActualSubmissions * 100 / totalPossibleSubmissions,
                    1
                );
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddStudentsAsync(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            if (SelectedStudentIds == null || !SelectedStudentIds.Any())
            {
                return RedirectToPage(new { id = id });
            }

            var classExists = await _context.Classes.AnyAsync(c =>
                c.Id == id && c.ManagerId == userId
            );
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

        public async Task<IActionResult> OnPostCreateGroupsAsync(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            if (NumberOfGroupsToCreate <= 0 || NumberOfGroupsToCreate > 6)
            {
                ModelState.AddModelError(
                    "NumberOfGroupsToCreate",
                    "Số lượng nhóm phải từ 1 đến 6."
                );
            }

            var groupValidationState = ModelState.GetFieldValidationState(
                nameof(NumberOfGroupsToCreate)
            );
            if (
                groupValidationState
                == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid
            )
            {
                var success = await LoadTeacherClassroomDataAsync(id, userId);
                if (!success)
                {
                    return NotFound();
                }

                await LoadDashboardStatsAsync(id);
                ViewData["ShowCreateGroupModal"] = true;
                return Page();
            }

            var classExists = await _context
                .Classes.Include(c => c.Groups)
                .FirstOrDefaultAsync(c => c.Id == id && c.ManagerId == userId);
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

                var groupIdsToRemove = groupsToRemove.Select(g => g.Id).ToList();

                // 1. Clear GroupMembers referencing these groups
                var membersToRemove = await _context
                    .GroupMembers.Where(gm => groupIdsToRemove.Contains(gm.GroupId))
                    .ToListAsync();
                if (membersToRemove.Any())
                {
                    _context.GroupMembers.RemoveRange(membersToRemove);
                }

                // 2. Set GroupId to null for Submissions referencing these groups
                var submissionsToNullify = await _context
                    .Submissions.Where(s =>
                        s.GroupId.HasValue && groupIdsToRemove.Contains(s.GroupId.Value)
                    )
                    .ToListAsync();
                foreach (var sub in submissionsToNullify)
                {
                    sub.GroupId = null;
                }

                // 3. Remove the groups
                foreach (var g in groupsToRemove)
                {
                    classExists.Groups.Remove(g);
                }
                _context.Groups.RemoveRange(groupsToRemove);
            }

            var allGroups = classExists
                .Groups.OrderBy(g => g.Id == 0 ? int.MaxValue : g.Id)
                .ToList();
            for (int i = 0; i < allGroups.Count; i++)
            {
                allGroups[i].GroupName = $"Nhóm {i + 1}";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAssignGroupAsync(int id, int studentId, int? groupId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            var isManager = await _context.Classes.AnyAsync(c =>
                c.Id == id && c.ManagerId == userId
            );
            if (!isManager)
            {
                return Forbid();
            }

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
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            var isManager = await _context.Classes.AnyAsync(c =>
                c.Id == id && c.ManagerId == userId
            );
            if (!isManager)
            {
                return Forbid();
            }

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

        public async Task<IActionResult> OnPostCreateAssignmentAsync(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            var isManager = await _context.Classes.AnyAsync(c =>
                c.Id == id && c.ManagerId == userId
            );
            if (!isManager)
            {
                return Forbid();
            }

            if (NewAssignment == null || string.IsNullOrWhiteSpace(NewAssignment.Title))
            {
                ModelState.AddModelError("NewAssignment.Title", "Vui lòng nhập tiêu đề bài tập.");
            }
            else if (NewAssignment.Title.Length > 100)
            {
                ModelState.AddModelError(
                    "NewAssignment.Title",
                    "Tiêu đề bài tập không được vượt quá 100 ký tự."
                );
            }

            if (NewAssignment != null)
            {
                if (NewAssignment.DueDate == default(DateTime))
                {
                    ModelState.AddModelError("NewAssignment.DueDate", "Vui lòng chọn hạn nộp bài.");
                }
                else if (NewAssignment.DueDate.Date <= DateTime.Today)
                {
                    ModelState.AddModelError(
                        "NewAssignment.DueDate",
                        "Hạn nộp bài phải sau ngày hôm nay."
                    );
                }
            }

            var titleState = ModelState.GetFieldValidationState("NewAssignment.Title");
            var dueDateState = ModelState.GetFieldValidationState("NewAssignment.DueDate");

            if (
                titleState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid
                || dueDateState
                    == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid
            )
            {
                await LoadTeacherClassroomDataAsync(id, userId);
                await LoadDashboardStatsAsync(id);
                ViewData["ShowCreateAssignmentModal"] = true;
                return Page();
            }

            var assignment = new Assignment
            {
                ClassId = id,
                NodeId = NewAssignment.NodeId,
                Title = NewAssignment.Title,
                RequirementText = NewAssignment.RequirementText,
                Type = NewAssignment.Type ?? "File",
                DueDate = NewAssignment.DueDate,
                CreatedBy = userId,
                CreatedAt = DateTime.Now,
            };

            await _assignmentService.CreateAssignmentAsync(assignment);
            TempData["SuccessMessage"] = "Tạo bài tập thành công!";

            // Send notification emails to all students in the class
            try
            {
                var students = await _context
                    .ClassMembers.Include(cm => cm.User)
                    .Where(cm =>
                        cm.ClassId == id
                        && cm.User != null
                        && cm.User.Role == "Student"
                        && !string.IsNullOrEmpty(cm.User.Email)
                    )
                    .Select(cm => new { cm.User.Email, cm.User.Username })
                    .ToListAsync();

                if (students.Any())
                {
                    var classroom = await _context.Classes.FindAsync(id);
                    var className = classroom?.ClassName ?? "Lớp học";

                    foreach (var student in students)
                    {
                        var subject = $"[NihongoPortal] Bài tập mới - {assignment.Title}";
                        var bodyHtml =
                            $@"
<h2>Thông báo bài tập mới</h2>
<p>Chào <strong>{student.Username}</strong>,</p>
<p>Giáo viên đã đăng một bài tập mới trong lớp học <strong>{className}</strong>.</p>
<p><strong>Thông tin bài tập:</strong></p>
<ul>
    <li><strong>Tiêu đề:</strong> {assignment.Title}</li>
    <li><strong>Loại bài tập:</strong> {assignment.Type}</li>
    <li><strong>Hạn nộp:</strong> {(assignment.DueDate.HasValue ? assignment.DueDate.Value.ToString("dd/MM/yyyy HH:mm") : "Không có hạn chót")}</li>
</ul>
<p>Vui lòng đăng nhập vào Nihongo Portal để xem chi tiết yêu cầu và nộp bài đúng hạn.</p>
<p>Trân trọng,<br/>Zenith Education</p>";

                        // Send asynchronously in a background task to prevent blocking the UI thread
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _authService.SendEmailAsync(
                                    student.Email!,
                                    subject,
                                    bodyHtml
                                );
                            }
                            catch (Exception)
                            {
                                // Fail silently inside background tasks
                            }
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Gracefully fail database queries so page does not crash on email failure
            }

            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostDeleteAssignmentAsync(int id, int assignmentId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            var isManager = await _context.Classes.AnyAsync(c =>
                c.Id == id && c.ManagerId == userId
            );
            if (!isManager)
            {
                return Forbid();
            }

            // Xóa file nộp bài liên quan để tiết kiệm dung lượng
            var submissions = await _submissionService.GetSubmissionsByAssignmentAsync(
                assignmentId
            );
            foreach (var sub in submissions)
            {
                if (!string.IsNullOrEmpty(sub.MediaUrl))
                {
                    var filePath = Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        sub.MediaUrl.TrimStart('/')
                    );
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            await _assignmentService.DeleteAssignmentAsync(assignmentId);
            TempData["SuccessMessage"] = "Xóa bài tập thành công!";

            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostGradeSubmissionAsync(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            var isManager = await _context.Classes.AnyAsync(c =>
                c.Id == id && c.ManagerId == userId
            );
            if (!isManager)
            {
                return Forbid();
            }

            if (GradeRequest == null)
            {
                ModelState.AddModelError("GradeRequest.Score", "Yêu cầu chấm điểm không hợp lệ.");
                await LoadTeacherClassroomDataAsync(id, userId);
                await LoadDashboardStatsAsync(id);
                return Page();
            }

            if (GradeRequest.Score < 0m || GradeRequest.Score > 10m)
            {
                ModelState.AddModelError(
                    "GradeRequest.Score",
                    "Điểm số phải nằm trong khoảng từ 0.0 đến 10.0."
                );
                await LoadTeacherClassroomDataAsync(id, userId);
                await LoadDashboardStatsAsync(id);
                ViewData["ShowGradeSubmissionModal"] = true;
                ViewData["GradeSubmissionId"] = GradeRequest.SubmissionId;
                ViewData["GradeScore"] = GradeRequest.Score;
                ViewData["GradeFeedback"] = GradeRequest.Feedback;
                return Page();
            }

            var success = await _submissionService.GradeSubmissionAsync(
                GradeRequest.SubmissionId,
                GradeRequest.Score,
                GradeRequest.Feedback
            );
            if (!success)
            {
                ModelState.AddModelError("GradeRequest.Score", "Không tìm thấy bài nộp.");
                await LoadTeacherClassroomDataAsync(id, userId);
                await LoadDashboardStatsAsync(id);
                return Page();
            }

            TempData["SuccessMessage"] = "Chấm điểm thành công!";
            return RedirectToPage(new { id = id });
        }

        private async Task<bool> LoadTeacherClassroomDataAsync(int id, int userId)
        {
            var classroom = await _context
                .Classes.Include(c => c.Manager)
                .Include(c => c.ClassMembers)
                    .ThenInclude(cm => cm.User)
                .Include(c => c.Groups)
                    .ThenInclude(g => g.GroupMembers)
                .Include(c => c.Nodes)
                .Include(c => c.Curriculum)
                    .ThenInclude(cu => cu!.Nodes)
                        .ThenInclude(n => n.Materials)
                .Include(c => c.Curriculum)
                    .ThenInclude(cu => cu!.Nodes)
                        .ThenInclude(n => n.Quizzes)
                            .ThenInclude(q => q.QuizQuestions)
                .Include(c => c.ClassSchedules)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
            {
                return false;
            }

            if (classroom.ManagerId != userId)
            {
                return false;
            }

            Class = classroom;

            var existingMemberIds = classroom.ClassMembers.Select(cm => cm.UserId).ToList();
            var availableUsers = await _context
                .Users.Where(u => !existingMemberIds.Contains(u.Id) && u.Role == "Student")
                .OrderBy(u => u.Username)
                .ToListAsync();

            AvailableUsers = availableUsers;
            NumberOfGroupsToCreate = classroom.Groups?.Count ?? 0;

            // Nạp danh sách bài tập và bài nộp
            Assignments = await _assignmentService.GetAssignmentsByClassAsync(id);
            AssignmentSubmissions = new Dictionary<int, List<Submission>>();

            foreach (var assign in Assignments)
            {
                var submissions = await _submissionService.GetSubmissionsByAssignmentAsync(
                    assign.Id
                );
                AssignmentSubmissions[assign.Id] = submissions;
            }

            // Nạp danh sách Hỏi & Đáp (Q&A Threads)
            QaThreads = await _context
                .QaThreads.Include(t => t.Student)
                .Include(t => t.QaReplies)
                    .ThenInclude(r => r.User)
                .Where(t => t.ClassId == id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return true;
        }

        private async Task LoadDashboardStatsAsync(int classId)
        {
            // Trạng thái mở/khóa node của lớp này
            NodeUnlockStatus = await _lessonService.GetNodeUnlockStatusAsync(classId);

            // Tổng số học sinh và số HS hoàn thành mỗi node
            TotalStudents = Class.ClassMembers?.Count ?? 0;
            NodeCompletionCounts = await _context
                .StudentProgresses.AsNoTracking()
                .Where(p => p.ClassId == classId && p.IsCompleted == true)
                .GroupBy(p => p.NodeId)
                .Select(g => new { NodeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.NodeId, x => x.Count);

            // Tiến độ chi tiết từng học sinh trong lớp
            var progressList = await _context
                .StudentProgresses.AsNoTracking()
                .Where(p => p.ClassId == classId && p.IsCompleted == true)
                .Select(p => new
                {
                    p.StudentId,
                    p.NodeId,
                    p.CompletedAt,
                })
                .ToListAsync();

            StudentProgressMap = progressList
                .GroupBy(p => p.StudentId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.NodeId, x => x.CompletedAt));

            // Tính toán thống kê cho dashboard
            if (Class.Curriculum != null && Class.Curriculum.Nodes != null)
            {
                var lessonNodes = Class
                    .Curriculum.Nodes.Where(n => n.ParentNodeId != null)
                    .ToList();
                TotalLessonsCount = lessonNodes.Count;

                if (TotalStudents > 0 && TotalLessonsCount > 0)
                {
                    var totalPossibleCompletions = TotalStudents * TotalLessonsCount;
                    var completedLessonNodeIds = lessonNodes.Select(n => n.Id).ToList();
                    var actualCompletions = progressList.Count(p =>
                        completedLessonNodeIds.Contains(p.NodeId)
                    );
                    AvgClassProgress = Math.Round(
                        (double)actualCompletions * 100 / totalPossibleCompletions,
                        1
                    );
                }
            }

            if (Assignments != null && Assignments.Any() && TotalStudents > 0)
            {
                var totalPossibleSubmissions = Assignments.Count * TotalStudents;
                var totalActualSubmissions = AssignmentSubmissions.Values.Sum(list => list.Count);
                AvgSubmissionRate = Math.Round(
                    (double)totalActualSubmissions * 100 / totalPossibleSubmissions,
                    1
                );
            }
        }

        // Mở / khóa một node cho lớp này. Chỉ giáo viên quản lý lớp mới được thao tác.
        // Trả về JSON để JS cập nhật giao diện mà không reload trang.
        public async Task<IActionResult> OnPostToggleNodeLockAsync(int id, int nodeId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(
                    new { success = false, message = "Phiên đăng nhập đã hết hạn." }
                )
                {
                    StatusCode = 401,
                };
            }

            var classroom = await _context.Classes.FirstOrDefaultAsync(c => c.Id == id);
            if (classroom == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy lớp học." })
                {
                    StatusCode = 404,
                };
            }

            if (classroom.ManagerId != userId)
            {
                return new JsonResult(
                    new { success = false, message = "Bạn không có quyền thao tác lớp này." }
                )
                {
                    StatusCode = 403,
                };
            }

            await _lessonService.ToggleNodeLockAsync(id, nodeId);
            var isUnlocked = await _lessonService.IsNodeUnlockedAsync(id, nodeId);

            return new JsonResult(new { success = true, isUnlocked });
        }

        public async Task<IActionResult> OnPostCreateThreadAsync(int id, string questionText)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(
                    new { success = false, message = "Phiên đăng nhập đã hết hạn." }
                )
                {
                    StatusCode = 401,
                };
            }

            var isManager = await _context.Classes.AnyAsync(c =>
                c.Id == id && c.ManagerId == userId
            );
            var isMember =
                isManager
                || await _context.ClassMembers.AnyAsync(cm =>
                    cm.ClassId == id && cm.UserId == userId
                );
            if (!isMember)
            {
                return new JsonResult(
                    new { success = false, message = "Bạn không có quyền trong lớp học này." }
                )
                {
                    StatusCode = 403,
                };
            }

            if (string.IsNullOrWhiteSpace(questionText))
            {
                return new JsonResult(
                    new { success = false, message = "Nội dung câu hỏi không được để trống." }
                );
            }

            var thread = new QaThread
            {
                ClassId = id,
                StudentId = userId,
                QuestionText = questionText.Trim(),
                CreatedAt = DateTime.Now,
                UpvoteCount = 0,
            };

            _context.QaThreads.Add(thread);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            return new JsonResult(
                new
                {
                    success = true,
                    thread = new
                    {
                        id = thread.Id,
                        studentName = user?.Username ?? "Unknown",
                        questionText = thread.QuestionText,
                        createdAtFormatted = thread.CreatedAt?.ToString("dd/MM/yyyy HH:mm")
                            ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    },
                }
            );
        }

        public async Task<IActionResult> OnPostCreateReplyAsync(
            int id,
            int threadId,
            string replyText
        )
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(
                    new { success = false, message = "Phiên đăng nhập đã hết hạn." }
                )
                {
                    StatusCode = 401,
                };
            }

            var isManager = await _context.Classes.AnyAsync(c =>
                c.Id == id && c.ManagerId == userId
            );
            var isMember =
                isManager
                || await _context.ClassMembers.AnyAsync(cm =>
                    cm.ClassId == id && cm.UserId == userId
                );
            if (!isMember)
            {
                return new JsonResult(
                    new { success = false, message = "Bạn không có quyền trong lớp học này." }
                )
                {
                    StatusCode = 403,
                };
            }

            var thread = await _context.QaThreads.FirstOrDefaultAsync(t =>
                t.Id == threadId && t.ClassId == id
            );
            if (thread == null)
            {
                return new JsonResult(
                    new { success = false, message = "Không tìm thấy chủ đề thảo luận." }
                )
                {
                    StatusCode = 404,
                };
            }

            if (string.IsNullOrWhiteSpace(replyText))
            {
                return new JsonResult(
                    new { success = false, message = "Nội dung phản hồi không được để trống." }
                );
            }

            var reply = new QaReply
            {
                QaThreadId = threadId,
                UserId = userId,
                ReplyText = replyText.Trim(),
                CreatedAt = DateTime.Now,
            };

            _context.QaReplies.Add(reply);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            return new JsonResult(
                new
                {
                    success = true,
                    reply = new
                    {
                        id = reply.Id,
                        userName = user?.Username ?? "Unknown",
                        userRole = user?.Role ?? "Teacher",
                        replyText = reply.ReplyText,
                        createdAtFormatted = reply.CreatedAt?.ToString("dd/MM/yyyy HH:mm")
                            ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    },
                }
            );
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

            if (cls.ManagerId != userId)
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

            if (cls.ManagerId != userId)
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
