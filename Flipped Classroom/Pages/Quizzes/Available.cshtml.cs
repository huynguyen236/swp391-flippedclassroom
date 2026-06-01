using System.Security.Claims;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Student")]
    public class AvailableModel : PageModel
    {
        private readonly IQuizService _quizService;

        public AvailableModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public List<Quiz> Quizzes { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var studentId))
            {
                return Forbid();
            }

            Quizzes = await _quizService.GetPublishedQuizzesForStudentAsync(studentId);
            return Page();
        }
    }
}
