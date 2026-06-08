using System.Security.Claims;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Student")]
    public class TakeModel : PageModel
    {
        private readonly IQuizService _quizService;

        public TakeModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public Quiz Quiz { get; set; } = default!;

        [BindProperty]
        public Dictionary<int, int> SelectedOptions { get; set; } = new();

        [BindProperty]
        public Dictionary<int, string> TextAnswers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null)
            {
                return Forbid();
            }

            var quiz = await _quizService.GetPublishedQuizForStudentAsync(id, studentId.Value);
            if (quiz == null)
            {
                return NotFound();
            }

            Quiz = quiz;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null)
            {
                return Forbid();
            }

            var result = await _quizService.SubmitQuizAsync(id, studentId.Value, SelectedOptions, TextAnswers);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToPage(new { id });
            }

            TempData["SuccessMessage"] = $"{result.Message} Điểm: {result.Score}.";
            return RedirectToPage("/Quizzes/Available");
        }

        private int? GetCurrentStudentId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var studentId) ? studentId : null;
        }
    }
}
