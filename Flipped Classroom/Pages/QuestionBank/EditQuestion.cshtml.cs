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
    public class EditQuestionModel : PageModel
    {
        private readonly IQuestionService _questionService;
        private readonly Swp391NihongoContext _context;

        public EditQuestionModel(IQuestionService questionService, Swp391NihongoContext context)
        {
            _questionService = questionService;
            _context = context;
        }

        [BindProperty]
        public Question EditQuestion { get; set; } = null!;

        [BindProperty]
        public List<QuestionOption> Options { get; set; } = new();

        public SelectList NodeSelectList { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var result = await _questionService.GetQuestionByIdAsync(id);
            if (result == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy câu hỏi hoặc câu hỏi đã bị xóa.";
                return RedirectToPage("/QuestionBank/QuestionList");
            }

            EditQuestion = result;
            NormalizeLegacyQuestionType(EditQuestion);
            PrepareOptionsForDisplay();
            await LoadNodesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            NormalizeLegacyQuestionType(EditQuestion);

            // Validate NodeId
            if (EditQuestion.NodeId <= 0 || !await _context.Nodes.AnyAsync(n => n.Id == EditQuestion.NodeId))
            {
                ModelState.AddModelError(nameof(EditQuestion.NodeId), "Vui lòng chọn một chủ đề hợp lệ.");
                await LoadNodesAsync();
                return PageWithForm();
            }

            if (IsMcq(EditQuestion.QuestionType))
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

                if (!await _questionService.UpdateQuestionAsync(EditQuestion, Options.Take(4).ToList()))
                {
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật câu hỏi. Vui lòng thử lại.");
                    await LoadNodesAsync();
                    return PageWithForm();
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(EditQuestion.CorrectAnswer))
                {
                    ModelState.AddModelError(nameof(EditQuestion.CorrectAnswer), "Vui lòng nhập đáp án tham khảo.");
                    await LoadNodesAsync();
                    return PageWithForm();
                }

                if (!await _questionService.UpdateQuestionAsync(EditQuestion, new List<QuestionOption>()))
                {
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật câu hỏi. Vui lòng thử lại.");
                    await LoadNodesAsync();
                    return PageWithForm();
                }
            }

            TempData["SuccessMessage"] = "Cập nhật câu hỏi thành công!";
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

        private void PrepareOptionsForDisplay()
        {
            if (IsMcq(EditQuestion.QuestionType)
                && EditQuestion.QuestionOptions != null
                && EditQuestion.QuestionOptions.Any())
            {
                Options = EditQuestion.QuestionOptions.OrderBy(o => o.Id).Take(4).ToList();
            }

            // Luôn có 4 slot trong form (ẩn khi không phải MCQ) để đổi loại câu hỏi trên UI vẫn hoạt động
            EnsureMcqOptionSlots();
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

        private static void NormalizeLegacyQuestionType(Question question)
        {
            if (string.Equals(question.QuestionType, "Text", StringComparison.OrdinalIgnoreCase))
            {
                question.QuestionType = "Writing";
            }
        }

        private IActionResult PageWithForm()
        {
            EnsureMcqOptionSlots();
            return Page();
        }
    }
}
