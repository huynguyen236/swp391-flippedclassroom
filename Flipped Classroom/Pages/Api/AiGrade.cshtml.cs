using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.Api
{
    [Authorize(Roles = "Teacher,Manager,Admin")]
    public class AiGradeModel : PageModel
    {
        private readonly IAiService _aiService;
        private readonly Swp391NihongoContext _context;

        public AiGradeModel(IAiService aiService, Swp391NihongoContext context)
        {
            _aiService = aiService;
            _context = context;
        }

        public IActionResult OnGet() => NotFound();

        /// <summary>
        /// POST /Api/AiGrade?submissionId=xxx — AI chấm điểm một bài nộp
        /// </summary>
        public async Task<IActionResult> OnPostAsync(int submissionId)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId))
                {
                    return new JsonResult(new AiGradeResponse
                    {
                        Success = false,
                        Error = "Vui lòng đăng nhập lại."
                    });
                }

                // Xác thực quyền: teacher phải quản lý lớp chứa submission
                var submission = await _context.Submissions
                    .Include(s => s.Assignment)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                {
                    return new JsonResult(new AiGradeResponse
                    {
                        Success = false,
                        Error = "Không tìm thấy bài nộp."
                    });
                }

                var isManager = await _context.Classes
                    .AnyAsync(c => c.Id == submission.Assignment.ClassId && c.ManagerId == userId);

                if (!isManager)
                {
                    return new JsonResult(new AiGradeResponse
                    {
                        Success = false,
                        Error = "Bạn không có quyền chấm điểm bài nộp này."
                    });
                }

                var result = await _aiService.GradeSubmissionAsync(submissionId);

                return new JsonResult(new AiGradeResponse
                {
                    Success = true,
                    SuggestedScore = result.SuggestedScore,
                    Feedback = result.Feedback,
                    Reasoning = result.Reasoning
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new AiGradeResponse
                {
                    Success = false,
                    Error = $"Lỗi AI: {ex.Message}"
                });
            }
        }
    }
}
