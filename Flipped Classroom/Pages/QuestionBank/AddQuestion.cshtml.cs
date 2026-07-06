using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.QuestionBank
{
    [Authorize(Roles = "Admin,Manager")]
    public class AddQuestionModel : PageModel
    {
        private readonly IQuestionService _questionService;
        private readonly Swp391NihongoContext _context;

        public AddQuestionModel(IQuestionService questionService, Swp391NihongoContext context)
        {
            _questionService = questionService;
            _context = context;
        }

        [BindProperty]
        public Question NewQuestion { get; set; } = null!;

        [BindProperty]
        public List<QuestionOption> Options { get; set; } = new();

        public SelectList NodeSelectList { get; set; }

        public async Task OnGetAsync()
        {
            NewQuestion = new Question { QuestionType = "MCQ" };
            EnsureMcqOptionSlots();
            await LoadNodesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.Equals(NewQuestion.QuestionType, "Text", StringComparison.OrdinalIgnoreCase))
            {
                NewQuestion.QuestionType = "Writing";
            }

            // Validate NodeId
            if (NewQuestion.NodeId <= 0 || !await _context.Nodes.AnyAsync(n => n.Id == NewQuestion.NodeId))
            {
                ModelState.AddModelError(nameof(NewQuestion.NodeId), "Vui lòng chọn một chủ đề hợp lệ.");
                await LoadNodesAsync();
                return PageWithForm();
            }

            if (IsMcq(NewQuestion.QuestionType))
            {
                EnsureMcqOptionSlots();

                if (Options.Take(4).Any(o => string.IsNullOrWhiteSpace(o.OptionContent)))
                {
                    ModelState.AddModelError(string.Empty, "Câu trắc nghiệm (MCQ) phải có đủ 4 đáp án A, B, C, D.");
                    await LoadNodesAsync();
                    return PageWithForm();
                }

                if (!Options.Take(4).Any(o => o.IsCorrect))
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng chọn ít nhất một đáp án đúng.");
                    await LoadNodesAsync();
                    return PageWithForm();
                }

                if (!await _questionService.CreateQuestionAsync(NewQuestion, Options.Take(4).ToList()))
                {
                    ModelState.AddModelError(string.Empty, "Không lưu được câu hỏi. Kiểm tra dữ liệu.");
                    await LoadNodesAsync();
                    return PageWithForm();
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(NewQuestion.CorrectAnswer))
                {
                    ModelState.AddModelError(nameof(NewQuestion.CorrectAnswer), "Vui lòng nhập đáp án tham khảo.");
                    await LoadNodesAsync();
                    return PageWithForm();
                }

                if (!await _questionService.CreateQuestionAsync(NewQuestion, new List<QuestionOption>()))
                {
                    ModelState.AddModelError(string.Empty, "Không lưu được câu hỏi. Kiểm tra dữ liệu.");
                    await LoadNodesAsync();
                    return PageWithForm();
                }
            }

            TempData["SuccessMessage"] = "Thêm câu hỏi mới vào kho thành công!";
            return RedirectToPage("/QuestionBank/QuestionList");
        }

        private async Task LoadNodesAsync()
        {
            var nodes = await _context.Nodes
                .OrderBy(n => n.Title)
                .Select(n => new { n.Id, n.Title })
                .ToListAsync();

            NodeSelectList = new SelectList(nodes, "Id", "Title");
        }

        private void EnsureMcqOptionSlots()
        {
            while (Options.Count < 4)
            {
                Options.Add(new QuestionOption());
            }

            if (Options.Count > 4)
            {
                Options = Options.Take(4).ToList();
            }
        }

        private static bool IsMcq(string? questionType) =>
            string.Equals(questionType, "MCQ", StringComparison.OrdinalIgnoreCase);

        private IActionResult PageWithForm()
        {
            EnsureMcqOptionSlots();
            return Page();
        }
    }
}
