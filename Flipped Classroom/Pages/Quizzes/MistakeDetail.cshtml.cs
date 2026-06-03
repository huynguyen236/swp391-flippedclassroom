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
            Detail = await _quizService.GetQuestionMistakeDetailAsync(questionId);
            if (Detail == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
