using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Admin,Teacher")]
    public class MistakeDetailModel : PageModel
    {
        private readonly IQuizService _quizService;

        public MistakeDetailModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [BindProperty(SupportsGet = true)]
        public int? ReturnClassId { get; set; }

        public QuestionMistakeDetail? Detail { get; set; }

        public async Task<IActionResult> OnGetAsync(int questionId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? managerId = null;
            if (!User.IsInRole("Admin") && int.TryParse(userIdStr, out int userId))
            {
                managerId = userId;
            }

            Detail = await _quizService.GetQuestionMistakeDetailAsync(questionId, ReturnClassId, managerId);
            if (Detail == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostResolveAsync(int questionId)
        {
            if (ReturnClassId == null)
            {
                return BadRequest("Không xác định được lớp học.");
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? managerId = null;
            if (!User.IsInRole("Admin") && int.TryParse(userIdStr, out int userId))
            {
                managerId = userId;
            }

            try
            {
                await _quizService.ResolveQuestionMistakesForClassAsync(questionId, ReturnClassId.Value, managerId);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            TempData["SuccessMessage"] = "Đã đánh dấu câu hỏi là đã chữa cho lớp. Câu hỏi này sẽ tạm ẩn khỏi bảng thống kê.";
            return RedirectToPage("/Quizzes/MistakeStats", new { classId = ReturnClassId });
        }
    }
}
