using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flipped_Classroom.Services.Implementation
{
    public class AiService : IAiService
    {
        private readonly Swp391NihongoContext _context;
        private readonly HttpClient _httpClient;
        private readonly AiSettings _settings;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AiService> _logger;

        public AiService(
            Swp391NihongoContext context,
            HttpClient httpClient,
            IOptions<AiSettings> settings,
            IWebHostEnvironment env,
            ILogger<AiService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _settings = settings.Value;
            _env = env;
            _logger = logger;
        }

        // ─────────────────────────────────────────────
        //  AI GRADING
        // ─────────────────────────────────────────────

        public async Task<AiGradeResult> GradeSubmissionAsync(int submissionId)
        {
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
                throw new KeyNotFoundException($"Không tìm thấy bài nộp với Id = {submissionId}.");

            // Đọc nội dung bài nộp
            string submissionContent = "";

            // Đọc file .docx nếu có
            if (!string.IsNullOrEmpty(submission.MediaUrl) && submission.MediaUrl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                var filePath = Path.Combine(_env.WebRootPath, submission.MediaUrl.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    submissionContent = ExtractDocxText(filePath);
                }
            }

            // Nếu có ContentText (bài nộp text), kết hợp
            if (!string.IsNullOrEmpty(submission.ContentText))
            {
                submissionContent = string.IsNullOrEmpty(submissionContent)
                    ? submission.ContentText
                    : $"{submissionContent}\n\n--- Ghi chú của học viên ---\n{submission.ContentText}";
            }

            if (string.IsNullOrWhiteSpace(submissionContent))
            {
                return new AiGradeResult(0, "Không thể đọc nội dung bài nộp (file không phải .docx hoặc trống).", "Bài nộp không có nội dung text để chấm.");
            }

            // Lấy yêu cầu bài tập
            var requirementText = submission.Assignment?.RequirementText ?? "Không có mô tả yêu cầu.";
            var assignmentTitle = submission.Assignment?.Title ?? "Bài tập";

            var prompt = $@"Bạn là giáo viên tiếng Nhật chuyên nghiệp đang chấm bài tập trong hệ thống Flipped Classroom.

## Thông tin bài tập
- **Tiêu đề:** {assignmentTitle}
- **Yêu cầu:** {requirementText}

## Bài nộp của học viên
{submissionContent}

## Yêu cầu chấm điểm
Hãy chấm điểm bài nộp trên thang điểm 0.0 đến 10.0 và đưa ra nhận xét chi tiết.

Trả lời theo định dạng JSON chính xác sau (KHÔNG thêm markdown code block):
{{
  ""score"": <số thập phân từ 0.0 đến 10.0>,
  ""feedback"": ""<nhận xét tổng quan bằng tiếng Việt, 2-4 câu>"",
  ""reasoning"": ""<phân tích chi tiết bằng tiếng Việt: điểm mạnh, điểm cần cải thiện, lỗi cụ thể nếu có>""
}}";

            var response = await CallGeminiApiAsync(prompt);

            try
            {
                // Parse JSON response từ AI
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    using var doc = JsonDocument.Parse(jsonStr);
                    var root = doc.RootElement;

                    var score = root.GetProperty("score").GetDecimal();
                    score = Math.Clamp(score, 0m, 10m);
                    var feedback = root.GetProperty("feedback").GetString() ?? "";
                    var reasoning = root.GetProperty("reasoning").GetString() ?? "";

                    return new AiGradeResult(score, feedback, reasoning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể parse AI grading response: {Response}", response);
            }

            // Fallback nếu không parse được
            return new AiGradeResult(5.0m, response, "AI không trả về định dạng chuẩn. Phản hồi gốc được hiển thị trong feedback.");
        }

        // ─────────────────────────────────────────────
        //  AI CHATBOT
        // ─────────────────────────────────────────────

        public async Task<string> ChatAsync(int studentId, int? classId, int? nodeId, string userMessage, List<ChatMessage>? history)
        {
            // Thu thập context từ database
            var contextParts = new List<string>();

            // Context 1: Thông tin bài học hiện tại (nếu đang ở một node cụ thể)
            if (nodeId.HasValue)
            {
                var node = await _context.Set<Node>()
                    .Include(n => n.Vocabularies)
                    .Include(n => n.Materials)
                    .Include(n => n.Questions)
                        .ThenInclude(q => q.QuestionOptions)
                    .FirstOrDefaultAsync(n => n.Id == nodeId.Value);

                if (node != null)
                {
                    contextParts.Add($"📚 **Bài học hiện tại:** {node.Title}");
                    if (!string.IsNullOrEmpty(node.Description))
                        contextParts.Add($"Mô tả: {node.Description}");

                    if (node.Vocabularies.Any())
                    {
                        var vocabList = string.Join("\n", node.Vocabularies.Select(v =>
                            $"- {v.Word} ({v.Hiragana}) — {v.Meaning}" + (v.Romaji != null ? $" [{v.Romaji}]" : "")));
                        contextParts.Add($"📝 Từ vựng bài này:\n{vocabList}");
                    }

                    if (node.Materials.Any())
                    {
                        var materialsList = string.Join("\n", node.Materials.Select(m => $"- {m.Title} (Loại: {m.MaterialType})"));
                        contextParts.Add($"📖 Tài liệu:\n{materialsList}");
                    }
                }
            }

            // Context 2: Lỗi sai gần đây của học viên
            var recentMistakes = await _context.Set<StudentMistake>()
                .Include(m => m.Question)
                .Where(m => m.StudentId == studentId && m.IsResolved != true)
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToListAsync();

            if (recentMistakes.Any())
            {
                var mistakeList = string.Join("\n", recentMistakes.Select(m =>
                    $"- Câu hỏi: \"{m.Question?.Content ?? "N/A"}\" (Sai {m.ErrorCount ?? 1} lần, Loại lỗi: {m.MistakeType ?? "N/A"})"));
                contextParts.Add($"⚠️ Lỗi sai gần đây của học viên:\n{mistakeList}");
            }

            // Context 3: Kết quả quiz gần đây
            var recentResults = await _context.Set<QuizResult>()
                .Include(r => r.Quiz)
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CompletedAt)
                .Take(3)
                .ToListAsync();

            if (recentResults.Any())
            {
                var resultList = string.Join("\n", recentResults.Select(r =>
                    $"- Quiz \"{r.Quiz?.Title ?? "N/A"}\": {r.Score}/10 (Ngày: {r.CompletedAt?.ToString("dd/MM/yyyy") ?? "N/A"})"));
                contextParts.Add($"📊 Kết quả quiz gần đây:\n{resultList}");
            }

            var contextStr = contextParts.Any()
                ? string.Join("\n\n", contextParts)
                : "Không có context bài học cụ thể.";

            // Xây dựng prompt
            var systemPrompt = $@"Bạn là trợ lý AI thông minh tên là ""Nihongo Sensei"" trong hệ thống học tiếng Nhật Flipped Classroom (Nihongo Portal).

## Vai trò của bạn
- Hỗ trợ học viên học tiếng Nhật: giải thích ngữ pháp, từ vựng, kanji
- Trả lời câu hỏi liên quan đến bài học
- Phân tích lỗi sai và gợi ý cách cải thiện
- Cho bài tập luyện tập khi học viên yêu cầu
- Động viên và tạo hứng thú học tập

## Quy tắc
- Trả lời bằng tiếng Việt, kèm tiếng Nhật khi cần thiết
- Sử dụng emoji phù hợp để sinh động
- Câu trả lời ngắn gọn, dễ hiểu (tối đa 300 từ)
- Khi giải thích từ vựng, bao gồm: Kanji/Kana, Hiragana, Romaji, Nghĩa
- Khi được hỏi về bài học, ưu tiên sử dụng data từ context bên dưới

## Context hiện tại của học viên
{contextStr}";

            // Tạo conversation history cho Gemini
            var contents = new List<object>();

            // Thêm chat history nếu có
            if (history != null && history.Count > 0)
            {
                foreach (var msg in history.TakeLast(10)) // Giới hạn 10 tin nhắn gần nhất
                {
                    contents.Add(new
                    {
                        role = msg.Role == "user" ? "user" : "model",
                        parts = new[] { new { text = msg.Content } }
                    });
                }
            }

            // Thêm tin nhắn hiện tại
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            return await CallGeminiApiAsync(userMessage, systemPrompt, contents);
        }

        // ─────────────────────────────────────────────
        //  AI PHÂN TÍCH ĐIỂM YẾU
        // ─────────────────────────────────────────────

        public async Task<string> AnalyzeWeaknessesAsync(int studentId)
        {
            var mistakes = await _context.Set<StudentMistake>()
                .Include(m => m.Question)
                .Where(m => m.StudentId == studentId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(20)
                .ToListAsync();

            var quizResults = await _context.Set<QuizResult>()
                .Include(r => r.Quiz)
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CompletedAt)
                .Take(10)
                .ToListAsync();

            var mistakeData = mistakes.Any()
                ? string.Join("\n", mistakes.Select(m =>
                    $"- Câu: \"{m.Question?.Content ?? "N/A"}\" | Loại: {m.Question?.Category ?? "N/A"} | Sai {m.ErrorCount ?? 1} lần | Loại lỗi: {m.MistakeType ?? "N/A"} | Đã sửa: {(m.IsResolved == true ? "Có" : "Chưa")}"))
                : "Chưa có dữ liệu lỗi sai.";

            var quizData = quizResults.Any()
                ? string.Join("\n", quizResults.Select(r =>
                    $"- Quiz \"{r.Quiz?.Title ?? "N/A"}\": Điểm {r.Score}/10 | Ngày: {r.CompletedAt?.ToString("dd/MM/yyyy") ?? "N/A"}"))
                : "Chưa có dữ liệu quiz.";

            var prompt = $@"Bạn là giáo viên tiếng Nhật phân tích kết quả học tập của một học viên.

## Dữ liệu lỗi sai (20 gần nhất)
{mistakeData}

## Kết quả Quiz (10 gần nhất)
{quizData}

## Yêu cầu
Hãy phân tích ngắn gọn (tối đa 200 từ) bằng tiếng Việt:
1. **Xu hướng điểm**: Điểm quiz có xu hướng tăng hay giảm?
2. **Chủ đề yếu nhất**: Top 3 chủ đề/category cần ôn lại nhiều nhất
3. **Gợi ý cải thiện**: 2-3 lời khuyên cụ thể, thực tế

Sử dụng emoji phù hợp, viết ngắn gọn, dễ hiểu.";

            return await CallGeminiApiAsync(prompt);
        }

        // ─────────────────────────────────────────────
        //  AI GIẢI THÍCH CÂU HỎI
        // ─────────────────────────────────────────────

        public async Task<string> ExplainQuestionAsync(int questionId, int? selectedOptionId)
        {
            var question = await _context.Set<Question>()
                .Include(q => q.QuestionOptions)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
                return "Không tìm thấy câu hỏi.";

            var optionsList = question.QuestionOptions.Any()
                ? string.Join("\n", question.QuestionOptions.Select(o =>
                    $"- {o.OptionContent} {(o.IsCorrect ? "✅ (Đáp án đúng)" : "")}"))
                : "Không có tùy chọn.";

            var selectedOptionText = selectedOptionId.HasValue
                ? question.QuestionOptions.FirstOrDefault(o => o.Id == selectedOptionId.Value)?.OptionContent ?? "N/A"
                : "N/A";

            var prompt = $@"Bạn là giáo viên tiếng Nhật. Học viên vừa trả lời sai câu hỏi sau.

## Câu hỏi
{question.Content}

## Các lựa chọn
{optionsList}

## Học viên đã chọn
{selectedOptionText}

## Giải thích có sẵn
{question.Explanation ?? "Không có giải thích sẵn."}

## Yêu cầu
Hãy giải thích bằng tiếng Việt (tối đa 150 từ):
1. Tại sao đáp án đúng là đúng
2. Tại sao lựa chọn của học viên sai
3. Mẹo/quy tắc giúp nhớ
Sử dụng ví dụ tiếng Nhật nếu hữu ích. Sử dụng emoji phù hợp.";

            return await CallGeminiApiAsync(prompt);
        }

        // ─────────────────────────────────────────────
        //  GEMINI API CALLER
        // ─────────────────────────────────────────────

        private async Task<string> CallGeminiApiAsync(string prompt, string? systemInstruction = null, List<object>? contents = null)
        {
            if (string.IsNullOrEmpty(_settings.ApiKey))
                return "⚠️ AI chưa được cấu hình. Vui lòng thêm API key trong appsettings.json (section AiSettings).";

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.ModelName}:generateContent?key={_settings.ApiKey}";

            var requestBody = new Dictionary<string, object>();

            // System instruction
            if (!string.IsNullOrEmpty(systemInstruction))
            {
                requestBody["systemInstruction"] = new
                {
                    parts = new[] { new { text = systemInstruction } }
                };
            }

            // Contents
            if (contents != null && contents.Count > 0)
            {
                requestBody["contents"] = contents;
            }
            else
            {
                requestBody["contents"] = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                };
            }

            // Generation config
            requestBody["generationConfig"] = new
            {
                maxOutputTokens = _settings.MaxTokens,
                temperature = 0.7
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, httpContent);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                    return $"⚠️ AI tạm thời không khả dụng (HTTP {(int)response.StatusCode}). Vui lòng thử lại sau.";
                }

                using var doc = JsonDocument.Parse(responseJson);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return text ?? "AI không trả về nội dung.";
                }

                return "AI không trả về kết quả. Vui lòng thử lại.";
            }
            catch (TaskCanceledException)
            {
                return "⚠️ Yêu cầu AI đã hết thời gian chờ. Vui lòng thử lại.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Gemini API");
                return $"⚠️ Đã xảy ra lỗi khi gọi AI: {ex.Message}";
            }
        }

        // ─────────────────────────────────────────────
        //  HELPER: ĐỌC NỘI DUNG FILE .DOCX
        // ─────────────────────────────────────────────

        private string ExtractDocxText(string filePath)
        {
            try
            {
                using var wordDoc = WordprocessingDocument.Open(filePath, false);
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body == null) return string.Empty;

                var sb = new StringBuilder();
                foreach (var para in body.Elements<Paragraph>())
                {
                    sb.AppendLine(para.InnerText);
                }

                // Giới hạn 5000 ký tự để tránh token quá lớn
                var text = sb.ToString();
                if (text.Length > 5000)
                    text = text.Substring(0, 5000) + "\n... (nội dung đã bị cắt do quá dài)";

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể đọc file .docx: {FilePath}", filePath);
                return "[Không thể đọc file .docx]";
            }
        }
    }
}
