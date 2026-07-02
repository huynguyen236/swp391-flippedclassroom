using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Admin,Manager,Teacher")]
    public class RandomizeModel : PageModel
    {
        private readonly IQuizService _quizService;
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;

        public RandomizeModel(IQuizService quizService, Flipped_Classroom.Data.Swp391NihongoContext context)
        {
            _quizService = quizService;
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public CreateRandomQuizRequest Input { get; set; } = new()
        {
            QuestionCount = 20,
            DurationMinutes = 30,
            PublishNow = true
        };

        [BindProperty]
        public bool IsStrictMode { get; set; }

        public List<Class> ClassesList { get; set; } = new();
        public List<Node> NodesList { get; set; } = new();
        public List<Flipped_Classroom.Models.Curriculum> CurriculumsList { get; set; } = new();

        public List<Quiz> RecentQuizzes { get; set; } = new();

        public int? AvailableQuestionCount { get; set; }

        public async Task OnGetAsync(int? nodeId, int? classId, bool? reset)
        {
            if (reset == true)
            {
                TempData.Remove("Randomize_NodeId");
                TempData.Remove("Randomize_IsStrictMode");
                IsStrictMode = false;
                Input.NodeId = 0;
                Input.ClassId = null;
            }
            else
            {
                if (TempData.TryGetValue("Randomize_NodeId", out var tNodeId) && tNodeId is int tempNodeId)
                {
                    Input.NodeId = tempNodeId;
                }
                else if (nodeId.HasValue)
                {
                    Input.NodeId = nodeId.Value;
                }

                if (TempData.TryGetValue("Randomize_IsStrictMode", out var tStrictMode) && tStrictMode is bool tempStrictMode)
                {
                    IsStrictMode = tempStrictMode;
                }

                TempData.Keep("Randomize_NodeId");
                TempData.Keep("Randomize_IsStrictMode");
            }

            if (classId.HasValue && reset != true) Input.ClassId = classId.Value;

            await LoadPageDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (TempData.TryGetValue("Randomize_IsStrictMode", out var tStrictMode) && tStrictMode is bool tempStrictMode)
            {
                IsStrictMode = tempStrictMode;
            }

            TempData.Keep("Randomize_IsStrictMode");

            if (IsStrictMode && TempData.TryGetValue("Randomize_NodeId", out var tNodeId) && tNodeId is int tempNodeId)
            {
                Input.NodeId = tempNodeId;
                TempData.Keep("Randomize_NodeId");
            }

            Input.PublishNow = true;

            if (User.IsInRole("Teacher") && !Input.ClassId.HasValue)
            {
                ModelState.AddModelError("Input.ClassId", "Giảng viên bắt buộc phải chọn lớp học.");
            }

            await LoadPageDataAsync();

            if (Input.ClassId.HasValue && Input.NodeId > 0)
            {
                var selectedClass = ClassesList.FirstOrDefault(c => c.Id == Input.ClassId.Value);
                var selectedNode = NodesList.FirstOrDefault(n => n.Id == Input.NodeId);
                if (selectedClass != null && selectedNode != null && selectedClass.CurriculumId != selectedNode.CurriculumId)
                {
                    ModelState.AddModelError("Input.NodeId", "Bài học đã chọn không thuộc khung chương trình của lớp học.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Input.IsAlwaysOpen = !IsStrictMode; // Strict mode = false, Free mode = true

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
            NodesList = await _quizService.GetNodesAsync();

            int? managerId = null;
            if (User.IsInRole("Teacher"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    managerId = userId;
                }
            }

            ClassesList = await _quizService.GetClassesAsync(managerId);

            var query = _context.Curriculums.AsQueryable();
            if (managerId.HasValue)
            {
                query = query.Where(cu => cu.ManagerId == managerId.Value);
            }
            CurriculumsList = await query
                .OrderBy(cu => cu.CurriculumName)
                .ToListAsync();

            RecentQuizzes = await _quizService.GetRecentQuizzesAsync();
        }
    }
}
