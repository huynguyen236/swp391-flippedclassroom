using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface IQuizService
    {
        Task<List<Node>> GetNodesAsync();

        Task<List<Quiz>> GetPublishedQuizzesForStudentAsync(int studentId);

        Task<List<Quiz>> GetRecentQuizzesAsync();

        Task<Quiz?> GetPublishedQuizForStudentAsync(int quizId, int studentId);

        Task<int> CountAvailableQuestionsAsync(int nodeId, string category);

        Task<CreateRandomQuizResult> CreateRandomQuizAsync(CreateRandomQuizRequest request);

        Task<SubmitQuizResult> SubmitQuizAsync(int quizId, int studentId, Dictionary<int, int> selectedOptionIds, Dictionary<int, string> textAnswers);

        Task<List<StudentMistake>> GetDailyReviewMistakesAsync(int studentId, int questionCount);

        Task<bool> IsDailyReviewRequiredAsync(int studentId);

        Task<DailyReviewSubmitResult> SubmitDailyReviewAsync(int studentId, Dictionary<int, int> selectedOptionIds, Dictionary<int, string> textAnswers);

        Task<List<QuestionMistakeStatistic>> GetMistakeStatisticsAsync(int? classId);

        Task<QuestionMistakeDetail?> GetQuestionMistakeDetailAsync(int questionId, int? classId = null);

        Task ResolveQuestionMistakesForClassAsync(int questionId, int classId);

        Task<List<Class>> GetClassesAsync();

        Task CloneCurriculumQuizzesToClassAsync(int curriculumId, int classId);
    }

    public class CreateRandomQuizRequest
    {
        public int NodeId { get; set; }

        public int? ClassId { get; set; }

        public string Category { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public int QuestionCount { get; set; }

        public int DurationMinutes { get; set; }

        public bool PublishNow { get; set; }

        public bool IsAlwaysOpen { get; set; }
    }

    public class CreateRandomQuizResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? QuizId { get; set; }

        public int AvailableQuestionCount { get; set; }
    }

    public class SubmitQuizResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? QuizResultId { get; set; }

        public decimal Score { get; set; }

        public int TotalQuestions { get; set; }

        public int CorrectAnswers { get; set; }
    }

    public class QuestionMistakeStatistic
    {
        public int QuestionId { get; set; }

        public string QuestionContent { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string NodeTitle { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public int WrongStudentCount { get; set; }

        public int TotalMistakeCount { get; set; }

        public int ClassStudentCount { get; set; }

        public decimal WrongStudentPercent { get; set; }
    }

    public class QuestionMistakeDetail
    {
        public int QuestionId { get; set; }

        public string QuestionContent { get; set; } = string.Empty;

        public string QuestionType { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string? CorrectAnswer { get; set; }

        public string? Explanation { get; set; }

        public string NodeTitle { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;

        public int ClassId { get; set; }

        public int WrongStudentCount { get; set; }

        public int TotalMistakeCount { get; set; }

        public int ClassStudentCount { get; set; }

        public decimal WrongStudentPercent { get; set; }

        public List<QuestionOptionDetail> Options { get; set; } = new();

        public List<StudentMistakeEntry> Students { get; set; } = new();
    }

    public class QuestionOptionDetail
    {
        public int Id { get; set; }

        public string OptionContent { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }

    public class StudentMistakeEntry
    {
        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public int ErrorCount { get; set; }

        public string? MistakeType { get; set; }

        public DateOnly? NextReviewDate { get; set; }
    }

    public class DailyReviewSubmitResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int ReviewedCount { get; set; }

        public int CorrectCount { get; set; }

        public int MasteredCount { get; set; }
    }
}
