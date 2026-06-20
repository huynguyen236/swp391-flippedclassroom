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

namespace Flipped_Classroom.Pages.StudentClasses
{
    [Authorize(Roles = "Student,Teacher")]
    public class LessonModel : PageModel
    {
        private readonly ILessonService _lessonService;
        private readonly Swp391NihongoContext _context;

        public LessonModel(ILessonService lessonService, Swp391NihongoContext context)
        {
            _lessonService = lessonService;
            _context = context;
        }

        public Class Class { get; set; } = default!;
        public Node Lesson { get; set; } = default!;
        public List<QaThread> QaThreads { get; set; } = new();

        // Học sinh hiện tại đã tự đánh dấu hoàn thành bài này chưa
        public bool IsCompleted { get; set; }
        public bool IsStudentView { get; set; }

        public async Task<IActionResult> OnGetAsync(int classId, int nodeId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            var classroom = await _lessonService.GetClassWithMembersAsync(classId);
            if (classroom == null)
            {
                return NotFound();
            }

            var isTeacher = User.IsInRole("Teacher");
            if (isTeacher)
            {
                if (classroom.ManagerId != userId) return Forbid();
            }
            else
            {
                if (!classroom.ClassMembers.Any(cm => cm.UserId == userId)) return Forbid();
            }

            var node = await _lessonService.GetNodeWithMaterialsAsync(nodeId);
            if (node == null)
            {
                return NotFound();
            }

            if (node.CurriculumId != classroom.CurriculumId)
            {
                return Forbid();
            }

            // Chốt chặn backend: học sinh chỉ vào được node đã mở.
            // UI đã làm xám/disable bài khóa, đây chỉ để chặn HS gõ thẳng URL.
            // Giáo viên quản lý lớp luôn xem được (để kiểm tra nội dung trước khi mở).
            if (!isTeacher)
            {
                var unlocked = await _lessonService.IsNodeUnlockedAsync(classId, nodeId);
                if (!unlocked)
                {
                    return Forbid();
                }

                IsStudentView = true;
                IsCompleted = await _lessonService.IsNodeCompletedAsync(classId, nodeId, userId);
            }

            Class = classroom;
            Lesson = node;



            // Nạp danh sách Hỏi & Đáp (Q&A/Lesson Comments) cho bài học này
            QaThreads = await _context.QaThreads
                .Include(t => t.Student)
                .Include(t => t.QaReplies)
                    .ThenInclude(r => r.User)
                .Where(t => t.ClassId == classId && t.NodeId == nodeId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Page();
        }

        // Học sinh tự tích / bỏ tích "đã hoàn thành"
        // Học sinh tự tích / bỏ tích "đã hoàn thành".
        // Trả về JSON để JS cập nhật giao diện mà không reload trang.
        public async Task<IActionResult> OnPostToggleCompleteAsync(int classId, int nodeId, bool isCompleted)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Phiên đăng nhập đã hết hạn." }) { StatusCode = 401 };
            }

            // Chỉ học sinh thuộc lớp và node đã mở mới được đánh dấu
            if (User.IsInRole("Teacher"))
            {
                return new JsonResult(new { success = false, message = "Giáo viên không thể đánh dấu hoàn thành." }) { StatusCode = 403 };
            }

            var classroom = await _lessonService.GetClassWithMembersAsync(classId);
            if (classroom == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy lớp học." }) { StatusCode = 404 };
            }

            if (!classroom.ClassMembers.Any(cm => cm.UserId == userId))
            {
                return new JsonResult(new { success = false, message = "Bạn không thuộc lớp học này." }) { StatusCode = 403 };
            }

            if (!await _lessonService.IsNodeUnlockedAsync(classId, nodeId))
            {
                return new JsonResult(new { success = false, message = "Bài học chưa được mở." }) { StatusCode = 403 };
            }

            await _lessonService.SetNodeCompletionAsync(classId, nodeId, userId, isCompleted);
            return new JsonResult(new { success = true, isCompleted });
        }

        public static string ToYouTubeEmbed(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            var watchMarker = "watch?v=";
            var watchIndex = url.IndexOf(watchMarker);
            if (watchIndex >= 0)
            {
                var id = url.Substring(watchIndex + watchMarker.Length);
                var amp = id.IndexOf('&');
                if (amp >= 0) id = id.Substring(0, amp);
                return $"https://www.youtube.com/embed/{id}";
            }

            var shortMarker = "youtu.be/";
            var shortIndex = url.IndexOf(shortMarker);
            if (shortIndex >= 0)
            {
                var id = url.Substring(shortIndex + shortMarker.Length);
                var q = id.IndexOf('?');
                if (q >= 0) id = id.Substring(0, q);
                return $"https://www.youtube.com/embed/{id}";
            }

            return url;
        }

        public async Task<IActionResult> OnPostCreateCommentAsync(int classId, int nodeId, string questionText)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Phiên đăng nhập đã hết hạn." }) { StatusCode = 401 };
            }

            var classroom = await _lessonService.GetClassWithMembersAsync(classId);
            if (classroom == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy lớp học." }) { StatusCode = 404 };
            }

            var isTeacher = User.IsInRole("Teacher");
            var isAuthorized = isTeacher 
                ? classroom.ManagerId == userId 
                : classroom.ClassMembers.Any(cm => cm.UserId == userId);

            if (!isAuthorized)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền trong lớp học này." }) { StatusCode = 403 };
            }

            if (string.IsNullOrWhiteSpace(questionText))
            {
                return new JsonResult(new { success = false, message = "Nội dung bình luận không được để trống." });
            }

            var thread = new QaThread
            {
                ClassId = classId,
                NodeId = nodeId,
                StudentId = userId,
                QuestionText = questionText.Trim(),
                CreatedAt = DateTime.Now,
                UpvoteCount = 0
            };

            _context.QaThreads.Add(thread);
            await _context.SaveChangesAsync();

            var dbUser = await _context.Users.FindAsync(userId);

            return new JsonResult(new
            {
                success = true,
                comment = new
                {
                    id = thread.Id,
                    studentName = dbUser?.Username ?? "Ẩn danh",
                    userRole = dbUser?.Role ?? "Student",
                    questionText = thread.QuestionText,
                    createdAtFormatted = thread.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? ""
                }
            });
        }

        public async Task<IActionResult> OnPostCreateReplyAsync(int classId, int threadId, string replyText)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Phiên đăng nhập đã hết hạn." }) { StatusCode = 401 };
            }

            var classroom = await _lessonService.GetClassWithMembersAsync(classId);
            if (classroom == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy lớp học." }) { StatusCode = 404 };
            }

            var isTeacher = User.IsInRole("Teacher");
            var isAuthorized = isTeacher 
                ? classroom.ManagerId == userId 
                : classroom.ClassMembers.Any(cm => cm.UserId == userId);

            if (!isAuthorized)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền trong lớp học này." }) { StatusCode = 403 };
            }

            if (string.IsNullOrWhiteSpace(replyText))
            {
                return new JsonResult(new { success = false, message = "Nội dung phản hồi không được để trống." });
            }

            var threadExists = await _context.QaThreads.AnyAsync(t => t.Id == threadId && t.ClassId == classId);
            if (!threadExists)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy chủ đề bình luận." }) { StatusCode = 404 };
            }

            var reply = new QaReply
            {
                QaThreadId = threadId,
                UserId = userId,
                ReplyText = replyText.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.QaReplies.Add(reply);
            await _context.SaveChangesAsync();

            var dbUser = await _context.Users.FindAsync(userId);

            return new JsonResult(new
            {
                success = true,
                reply = new
                {
                    id = reply.Id,
                    userName = dbUser?.Username ?? "Ẩn danh",
                    userRole = dbUser?.Role ?? "Student",
                    replyText = reply.ReplyText,
                    createdAtFormatted = reply.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? ""
                }
            });
        }

        public async Task<IActionResult> OnPostDeleteCommentAsync(int classId, int threadId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Phiên đăng nhập đã hết hạn." }) { StatusCode = 401 };
            }

            var thread = await _context.QaThreads.FirstOrDefaultAsync(t => t.Id == threadId && t.ClassId == classId);
            if (thread == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy bình luận." }) { StatusCode = 404 };
            }

            var classroom = await _lessonService.GetClassWithMembersAsync(classId);
            if (classroom == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy lớp học." }) { StatusCode = 404 };
            }

            var isAuthor = thread.StudentId == userId;
            var isTeacher = User.IsInRole("Teacher") && classroom.ManagerId == userId;

            if (!isAuthor && !isTeacher)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền xóa bình luận này." }) { StatusCode = 403 };
            }

            var replies = _context.QaReplies.Where(r => r.QaThreadId == threadId);
            _context.QaReplies.RemoveRange(replies);
            _context.QaThreads.Remove(thread);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteReplyAsync(int classId, int replyId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Phiên đăng nhập đã hết hạn." }) { StatusCode = 401 };
            }

            var reply = await _context.QaReplies
                .Include(r => r.QaThread)
                .FirstOrDefaultAsync(r => r.Id == replyId && r.QaThread.ClassId == classId);
            if (reply == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy phản hồi." }) { StatusCode = 404 };
            }

            var classroom = await _lessonService.GetClassWithMembersAsync(classId);
            if (classroom == null)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy lớp học." }) { StatusCode = 404 };
            }

            var isAuthor = reply.UserId == userId;
            var isTeacher = User.IsInRole("Teacher") && classroom.ManagerId == userId;

            if (!isAuthor && !isTeacher)
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền xóa phản hồi này." }) { StatusCode = 403 };
            }

            _context.QaReplies.Remove(reply);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}
