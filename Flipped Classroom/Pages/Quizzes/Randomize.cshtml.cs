using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Admin,Manager,Teacher")]
    public class RandomizeModel : PageModel
    {
        private readonly IQuizService _quizService;

        public RandomizeModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [BindProperty]
        public CreateRandomQuizRequest Input { get; set; } = new()
        {
            QuestionCount = 20,
            DurationMinutes = 30,
            PublishNow = true
        };

        public List<SelectListItem> NodeOptions { get; set; } = new();

        public List<Quiz> RecentQuizzes { get; set; } = new();

        public int? AvailableQuestionCount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadPageDataAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _quizService.CreateRandomQuizAsync(Input);
            AvailableQuestionCount = result.AvailableQuestionCount;

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return Page();
            }

            TempData["SuccessMessage"] = $"{result.Message} Mã quiz: #{result.QuizId}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetAvailabilityAsync(int nodeId, string category)
        {
            if (nodeId <= 0 || string.IsNullOrWhiteSpace(category))
            {
                return new JsonResult(new { count = 0 });
            }

            var count = await _quizService.CountAvailableQuestionsAsync(nodeId, category);
            return new JsonResult(new { count });
        }

        private async Task LoadPageDataAsync()
        {
            var nodes = await _quizService.GetNodesAsync();
            NodeOptions = nodes
                .Select(n => new SelectListItem
                {
                    Value = n.Id.ToString(),
                    Text = $"{n.Title}"
                })
                .ToList();

            RecentQuizzes = await _quizService.GetRecentQuizzesAsync();
        }
    }
}
