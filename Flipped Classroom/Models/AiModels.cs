using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models
{
    /// <summary>
    /// Kết quả chấm điểm bởi AI cho một bài nộp (Submission).
    /// </summary>
    public record AiGradeResult(
        decimal SuggestedScore,
        string Feedback,
        string Reasoning
    );

    /// <summary>
    /// Một tin nhắn trong cuộc trò chuyện với AI Chatbot.
    /// </summary>
    public record ChatMessage(
        string Role,     // "user" hoặc "model"
        string Content,
        DateTime Timestamp
    );

    /// <summary>
    /// Cấu hình AI đọc từ appsettings.json section "AiSettings".
    /// </summary>
    public class AiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "gemini-2.0-flash";
        public int MaxTokens { get; set; } = 2048;
    }

    /// <summary>
    /// Request body gửi tới AI Chat API endpoint.
    /// </summary>
    public class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? ClassId { get; set; }
        public int? NodeId { get; set; }
        public List<ChatMessage>? History { get; set; }
    }

    /// <summary>
    /// Response từ AI Chat API endpoint.
    /// </summary>
    public class AiChatResponse
    {
        public string Reply { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Response từ AI Grade API endpoint.
    /// </summary>
    public class AiGradeResponse
    {
        public decimal SuggestedScore { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string Reasoning { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
