using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<bool> CreateQuestionAsync(Question question, List<QuestionOption> questionOption);

        Task<(List<Question> questions, int totalPages)> getQuestionAsync(
            string searchKeyword,
            string questionType,
            string category,
            int pageIndex,
            int pageSize
        );

        Task<bool> DeleteQuestionAsync(int questionId);

        Task<Question> GetQuestionByIdAsync(int questionId);

        Task<bool> UpdateQuestionAsync(Question question, List<QuestionOption> questionOptions);
    }
}
