using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Teacher")]
    public class MistakeStatsModel : PageModel
    {
        private readonly IQuizService _quizService;

        public MistakeStatsModel(IQuizService quizService)
        {
            _quizService = quizService;
        }

        [BindProperty(SupportsGet = true)]
        public int? ClassId { get; set; }

        public List<SelectListItem> ClassOptions { get; set; } = new();

        public List<QuestionMistakeStatistic> Statistics { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? managerId = null;
            if (!User.IsInRole("Admin") && int.TryParse(userIdStr, out int userId))
            {
                managerId = userId;
            }

            var classes = await _quizService.GetClassesAsync(managerId);
            ClassOptions = classes
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ClassName
                })
                .ToList();

            Statistics = await _quizService.GetMistakeStatisticsAsync(ClassId, managerId);
        }
    }
}
