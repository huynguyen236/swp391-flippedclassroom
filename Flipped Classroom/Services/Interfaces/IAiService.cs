using System.Collections.Generic;
using System.Threading.Tasks;
using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    /// <summary>
    /// Service tích hợp AI: chấm điểm tự động, chatbot hỗ trợ học tập, phân tích điểm yếu.
    /// </summary>
    public interface IAiService
    {
        /// <summary>
        /// AI chấm điểm bài nộp (đọc file .docx) dựa trên yêu cầu bài tập.
        /// Trả về điểm gợi ý + feedback + reasoning.
        /// </summary>
        Task<AiGradeResult> GradeSubmissionAsync(int submissionId);

        /// <summary>
        /// AI Chatbot trò chuyện hỗ trợ học tập.
        /// Context-aware: biết bài học, từ vựng, lỗi sai của học viên.
        /// </summary>
        Task<string> ChatAsync(int studentId, int? classId, int? nodeId, string userMessage, List<ChatMessage>? history);

        /// <summary>
        /// AI phân tích điểm yếu của học viên dựa trên StudentMistakes + QuizResults.
        /// </summary>
        Task<string> AnalyzeWeaknessesAsync(int studentId);

        /// <summary>
        /// AI giải thích tại sao câu trả lời sai và gợi ý cách nhớ.
        /// </summary>
        Task<string> ExplainQuestionAsync(int questionId, int? selectedOptionId);
    }
}
