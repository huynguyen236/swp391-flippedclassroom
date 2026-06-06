using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
        private readonly IAssignmentService _assignmentService;
        private readonly ISubmissionService _submissionService;

        public DetailsModel(
            Swp391NihongoContext context,
            IAssignmentService assignmentService,
            ISubmissionService submissionService)
        {
            _context = context;
            _assignmentService = assignmentService;
            _submissionService = submissionService;
        }

        public Class Class { get; set; } = default!; 
        public List<StudentAssignmentDto> Assignments { get; set; } = new();

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

            var isMember = await _context.ClassMembers.AnyAsync(cm => cm.ClassId == id && cm.UserId == userId);
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
                await _submissionService.SubmitAssignmentAsync(AssignmentId, userId, UploadedFile, ContentText);
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
            var classroom = await _context.Classes
                .Include(c => c.Manager)
                .Include(c => c.ClassMembers)
                    .ThenInclude(cm => cm.User)
                .Include(c => c.Groups)
                    .ThenInclude(g => g.GroupMembers)
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

                Assignments.Add(new StudentAssignmentDto(
                    assign.Id,
                    assign.Title,
                    assign.RequirementText,
                    assign.DueDate,
                    status,
                    mediaUrl,
                    score,
                    feedback
                ));
            }

            return true;
        }
    }
}
