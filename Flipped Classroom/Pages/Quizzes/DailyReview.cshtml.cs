using System.Linq;
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
        public Dictionary<string, string> SelectedOptions { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string> TextAnswers { get; set; } = new();

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

            var selectedOptionsInt = SelectedOptions
                .Where(kvp => int.TryParse(kvp.Key, out _) && int.TryParse(kvp.Value, out _))
                .ToDictionary(kvp => int.Parse(kvp.Key), kvp => int.Parse(kvp.Value));

            var textAnswersInt = TextAnswers
                .Where(kvp => int.TryParse(kvp.Key, out _))
                .ToDictionary(kvp => int.Parse(kvp.Key), kvp => kvp.Value ?? string.Empty);

            var result = await _quizService.SubmitDailyReviewAsync(studentId.Value, selectedOptionsInt, textAnswersInt);
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
