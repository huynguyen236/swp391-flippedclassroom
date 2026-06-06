using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.StudentClasses
{
    [Authorize(Roles = "Student,Teacher")]
    public class LessonModel : PageModel
    {
        private readonly ILessonService _lessonService;

        public LessonModel(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        public Class Class { get; set; } = default!;
        public Node Lesson { get; set; } = default!;

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

            if (classroom.CurriculumId == null || node.CurriculumId != classroom.CurriculumId)
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
    }
}
