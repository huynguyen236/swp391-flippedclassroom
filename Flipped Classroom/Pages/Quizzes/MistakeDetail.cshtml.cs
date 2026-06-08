using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Admin,Manager,Teacher")]
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
            Detail = await _quizService.GetQuestionMistakeDetailAsync(questionId, ReturnClassId);
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

            await _quizService.ResolveQuestionMistakesForClassAsync(questionId, ReturnClassId.Value);

            TempData["SuccessMessage"] = "Đã đánh dấu câu hỏi là đã chữa cho lớp. Câu hỏi này sẽ tạm ẩn khỏi bảng thống kê.";
            return RedirectToPage("/Quizzes/MistakeStats", new { classId = ReturnClassId });
        }
    }
}
