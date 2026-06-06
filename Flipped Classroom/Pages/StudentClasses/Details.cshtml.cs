using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.MyClasses
{
    [Authorize(Roles = "Student")]
    public class DetailsModel : PageModel
    {
        private readonly Swp391NihongoContext _context;
        private readonly ILessonService _lessonService;

        public DetailsModel(Swp391NihongoContext context, ILessonService lessonService)
        {
            _context = context;
            _lessonService = lessonService;
        }

        public Class Class { get; set; } = default!;

        // Trạng thái mở/khóa từng node trong lớp (nodeId -> đã mở chưa). Không có khóa = chưa mở.
        public Dictionary<int, bool> NodeUnlockStatus { get; set; } = new();

        // Tiến độ học của chính học sinh này (nodeId -> đã hoàn thành chưa)
        public Dictionary<int, bool> NodeCompletionStatus { get; set; } = new();

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
                .FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
            {
                return NotFound();
            }

            // Verify the student is actually in this class
            if (!classroom.ClassMembers.Any(cm => cm.UserId == userId))
            {
                return Forbid();
            }

            Class = classroom;

            // Trạng thái mở/khóa node của lớp + tiến độ của chính học sinh
            NodeUnlockStatus = await _lessonService.GetNodeUnlockStatusAsync(classroom.Id);
            NodeCompletionStatus = await _lessonService.GetNodeCompletionAsync(classroom.Id, userId);

            return Page();
        }
    }
}
