using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Flipped_Classroom.Models;

namespace Flipped_Classroom.Pages.QuestionBank
{
    public class AddQuestionModel : PageModel
    {
        private readonly IQuestionService _questionService;

        public AddQuestionModel(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        public Question NewQuestion { get; set; }
        public List<QuestionOption> Options { get; set; }


        public void OnGet()
        {
            NewQuestion = new Question();
            Options = new List<QuestionOption>
            {
                new QuestionOption(),
                new QuestionOption(),
                new QuestionOption(),
                new QuestionOption()
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Bước làm sạch sơ bộ: Xóa bỏ những option mà người dùng bỏ trống không nhập chữ
            var validOptions = Options.Where(o => !string.IsNullOrWhiteSpace(o.OptionContent)).ToList();

            bool isSuccess = await _questionService.CreateQuestionAsync(NewQuestion, validOptions);

            if (isSuccess)
            {
                TempData["SuccessMessage"] = "Thêm câu hỏi mới vào kho thành công!";
                // Chuyển hướng về trang danh sách (View Question)
                return RedirectToPage("/QuestionBank/Index");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi lưu câu hỏi. Vui lòng thử lại.");
                return Page();
            }

        }
    }
}
