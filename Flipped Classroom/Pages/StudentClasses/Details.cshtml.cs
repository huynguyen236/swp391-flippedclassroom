using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.StudentClasses
{
    [Authorize(Roles = "Student")]
    public class DetailsModel : PageModel
    {
        private readonly Swp391NihongoContext _context;
        private readonly IAssignmentService _assignmentService;
        private readonly ISubmissionService _submissionService;
        private readonly ILessonService _lessonService;

        public DetailsModel(
            Swp391NihongoContext context,
            IAssignmentService assignmentService,
            ISubmissionService submissionService,
            ILessonService lessonService
        )
        {
            _context = context;
            _assignmentService = assignmentService;
            _submissionService = submissionService;
            _lessonService = lessonService;
        }

        public Class Class { get; set; } = default!;
        public List<StudentAssignmentDto> Assignments { get; set; } = new();
        public Dictionary<int, bool> NodeUnlockStatus { get; set; } = new();
        public Dictionary<int, bool> NodeCompletionStatus { get; set; } = new();
        public List<QaThread> QaThreads { get; set; } = new();

        [BindProperty]
        public int AssignmentId { get; set; }

        [BindProperty]
        public IFormFile UploadedFile { get; set; } = null!;

        [BindProperty]
        public string? ContentText { get; set; }

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

            var success = await LoadClassroomDataAsync(id.Value, userId);
            if (!success)
            {
                var classroomExists = await _context.Classes.AnyAsync(c => c.Id == id);
                if (classroomExists)
                {
                    return Forbid();
                }
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSubmitAssignmentAsync(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            var isMember = await _context.ClassMembers.AnyAsync(cm =>
                cm.ClassId == id && cm.UserId == userId
            );
            if (!isMember)
            {
                return Forbid();
            }

            if (UploadedFile == null)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng chọn tệp tin cần nộp.");
                await LoadClassroomDataAsync(id, userId);
                return Page();
            }

            try
            {
                await _submissionService.SubmitAssignmentAsync(
                    AssignmentId,
                    userId,
                    UploadedFile,
                    ContentText
                );
                TempData["SuccessMessage"] = "Nộp bài tập thành công!";
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadClassroomDataAsync(id, userId);
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi xảy ra khi nộp bài: {ex.Message}");
                await LoadClassroomDataAsync(id, userId);
                return Page();
            }

            return RedirectToPage(new { id = id });
        }

        private async Task<bool> LoadClassroomDataAsync(int id, int userId)
        {
            var classroom = await _context
                .Classes.Include(c => c.Curriculum)
                    .ThenInclude(curr => curr.Nodes)
                        .ThenInclude(n => n.Materials)
                .Include(c => c.Manager)
                .Include(c => c.ClassMembers)
                    .ThenInclude(cm => cm.User)
                .Include(c => c.Groups)
                    .ThenInclude(g => g.GroupMembers)
                .Include(c => c.ClassSchedules)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
            {
                return false;
            }

            if (!classroom.ClassMembers.Any(cm => cm.UserId == userId))
            {
                return false;
            }

            Class = classroom;

            // Tải trạng thái khóa/mở bài học và tiến độ hoàn thành
            NodeUnlockStatus = await _lessonService.GetNodeUnlockStatusAsync(id);
            NodeCompletionStatus = await _context
                .StudentProgresses.AsNoTracking()
                .Where(p => p.ClassId == id && p.StudentId == userId && p.IsCompleted == true)
                .ToDictionaryAsync(p => p.NodeId, p => true);

            // Tải danh sách bài tập của lớp
            var assignments = await _assignmentService.GetAssignmentsByClassAsync(id);
            Assignments = new List<StudentAssignmentDto>();

            foreach (var assign in assignments)
            {
                var submission = await _submissionService.GetSubmissionAsync(assign.Id, userId);
                string status = "Pending";
                string? mediaUrl = null;
                decimal? score = null;
                string? feedback = null;

                if (submission != null)
                {
                    status = submission.Status ?? "Submitted";
                    mediaUrl = submission.MediaUrl;
                    score = submission.Score;
                    feedback = submission.Feedback;
                }

                Assignments.Add(
                    new StudentAssignmentDto(
                        assign.Id,
                        assign.Title,
                        assign.RequirementText,
                        assign.DueDate,
                        status,
                        mediaUrl,
                        score,
                        feedback
                    )
                );
            }

            // Tải danh sách Hỏi & Đáp (Q&A Threads)
            QaThreads = await _context
                .QaThreads.Include(t => t.Student)
                .Include(t => t.QaReplies)
                    .ThenInclude(r => r.User)
                .Where(t => t.ClassId == id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return true;
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

            var isMember = await _context.ClassMembers.AnyAsync(cm =>
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

            var isMember = await _context.ClassMembers.AnyAsync(cm =>
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
                        userRole = user?.Role ?? "Student",
                        replyText = reply.ReplyText,
                        createdAtFormatted = reply.CreatedAt?.ToString("dd/MM/yyyy HH:mm")
                            ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    },
                }
            );
        }
    }
}
