using System.Security.Claims;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Student")]
    public class DailyReviewModel : PageModel
    {
        private readonly IQuizService _quizService;

        public DailyReviewModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public int QuestionCount { get; set; } = 5;

        [BindProperty]
        public Dictionary<int, int> SelectedOptions { get; set; } = new();

        [BindProperty]
        public Dictionary<int, string> TextAnswers { get; set; } = new();

        public List<StudentMistake> ReviewItems { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null)
            {
                return Forbid();
            }

            ReviewItems = await _quizService.GetDailyReviewMistakesAsync(studentId.Value, QuestionCount);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null)
            {
                return Forbid();
            }

            var result = await _quizService.SubmitDailyReviewAsync(studentId.Value, SelectedOptions, TextAnswers);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToPage();
            }

            TempData["SuccessMessage"] = $"{result.Message} Đúng {result.CorrectCount}/{result.ReviewedCount}, mastered {result.MasteredCount} câu.";
            return RedirectToPage();
        }

        private int? GetCurrentStudentId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var studentId) ? studentId : null;
        }
    }
}
