using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.QuestionBank
{
    public class EditQuestionModel : PageModel
    {
        private readonly IQuestionService _questionService;

        public EditQuestionModel(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        public Question EditQuestion { get; set; }

        public List<QuestionOption> Options { get; set; }


        public async Task<IActionResult> OnGetAsync(int id)
        {
            var result = await _questionService.GetQuestionByIdAsync(id);
            EditQuestion = result;
            if (EditQuestion == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy câu hỏi hoặc câu hỏi đã bị xóa.";
                return RedirectToPage("/QuestionBank/Index");
            }
            if (EditQuestion.QuestionOptions != null && EditQuestion.QuestionOptions.Any())
            {
                Options = EditQuestion.QuestionOptions.ToList();
            }


            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            // Bước làm sạch sơ bộ: Xóa bỏ những option mà người dùng bỏ trống không nhập chữ
            var validOptions = Options.Where(o => !string.IsNullOrWhiteSpace(o.OptionContent)).ToList();
            bool isSuccess = await _questionService.UpdateQuestionAsync(EditQuestion, validOptions);
            if (isSuccess)
            {
                TempData["SuccessMessage"] = "Cập nhật câu hỏi thành công!";
                // Chuyển hướng về trang danh sách (View Question)
                return RedirectToPage("/QuestionBank/Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi cập nhật câu hỏi. Vui lòng thử lại.");
                return Page();
            }
        }
    }
}
