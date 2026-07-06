using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.QuestionBank
{
    [Authorize(Roles = "Admin,Manager")]
    public class QuestionListModel : PageModel
    {
        private readonly IQuestionService _questionService;

        public QuestionListModel(IQuestionService questionService)
        {
            _questionService = questionService;
        }


        public List<Question> Questions { get; set; } = new List<Question>();

        [BindProperty(SupportsGet = true)]
        public string SearchKeyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string QuestionType { get; set; }

        [BindProperty(SupportsGet = true)]  
        public string Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public async Task OnGetAsync()
        {
            int pageSize = 10; // Default page size
            if (CurrentPage < 1) CurrentPage = 1; // Ensure current page is at least 1
            var result = await _questionService.getQuestionAsync(SearchKeyword, QuestionType, Category, CurrentPage, pageSize);
            Questions.Clear();
            Questions = result.questions;
            TotalPages = result.totalPages;
        }

        public async Task<IActionResult> OnPostDeleteAsync(int questionId)
        {
            bool isDeleted = await _questionService.DeleteQuestionAsync(questionId);
            if (isDeleted)
            {
                TempData["SuccessMessage"] = "Câu hỏi đã được xóa thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa câu hỏi. Vui lòng thử lại.";
            }
            return RedirectToPage(new 
            {
                SearchKeyword = this.SearchKeyword,
                QuestionType = this.QuestionType,
                Category = this.Category,
                CurrentPage = this.CurrentPage
            });
        }   
    } 
}
