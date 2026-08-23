using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.Api
{
    [Authorize]
    public class AiChatModel : PageModel
    {
        private readonly IAiService _aiService;

        public AiChatModel(IAiService aiService)
        {
            _aiService = aiService;
        }

        public IActionResult OnGet() => NotFound();

        /// <summary>
        /// POST /Api/AiChat — AI Chatbot endpoint
        /// Body: { message, classId?, nodeId?, history? }
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int studentId))
                {
                    return new JsonResult(new AiChatResponse
                    {
                        Success = false,
                        Error = "Vui lòng đăng nhập lại."
                    });
                }

                // Parse request body
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                var request = JsonSerializer.Deserialize<AiChatRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null || string.IsNullOrWhiteSpace(request.Message))
                {
                    return new JsonResult(new AiChatResponse
                    {
                        Success = false,
                        Error = "Tin nhắn không được để trống."
                    });
                }

                // Giới hạn độ dài tin nhắn
                if (request.Message.Length > 1000)
                {
                    return new JsonResult(new AiChatResponse
                    {
                        Success = false,
                        Error = "Tin nhắn quá dài (tối đa 1000 ký tự)."
                    });
                }

                var reply = await _aiService.ChatAsync(
                    studentId,
                    request.ClassId,
                    request.NodeId,
                    request.Message,
                    request.History
                );

                return new JsonResult(new AiChatResponse
                {
                    Success = true,
                    Reply = reply
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new AiChatResponse
                {
                    Success = false,
                    Error = $"Đã xảy ra lỗi: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// POST /Api/AiChat?handler=Explain — AI giải thích câu hỏi
        /// Query: questionId, selectedOptionId?
        /// </summary>
        public async Task<IActionResult> OnPostExplainAsync(int questionId, int? selectedOptionId)
        {
            try
            {
                var reply = await _aiService.ExplainQuestionAsync(questionId, selectedOptionId);
                return new JsonResult(new AiChatResponse
                {
                    Success = true,
                    Reply = reply
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new AiChatResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// POST /Api/AiChat?handler=Weakness — Phân tích điểm yếu
        /// </summary>
        public async Task<IActionResult> OnPostWeaknessAsync()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int studentId))
                {
                    return new JsonResult(new AiChatResponse
                    {
                        Success = false,
                        Error = "Vui lòng đăng nhập lại."
                    });
                }

                var reply = await _aiService.AnalyzeWeaknessesAsync(studentId);
                return new JsonResult(new AiChatResponse
                {
                    Success = true,
                    Reply = reply
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new AiChatResponse
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }
    }
}
