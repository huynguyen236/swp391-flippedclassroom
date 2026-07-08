using System.Security.Claims;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Student")]
    public class TakeModel : PageModel
    {
        private readonly IQuizService _quizService;
        private readonly IDataProtector _protector;
        private readonly Swp391NihongoContext _context;

        public TakeModel(IQuizService quizService, IDataProtectionProvider protectionProvider, Swp391NihongoContext context)
        {
            _quizService = quizService;
            _protector = protectionProvider.CreateProtector("FlippedClassroom.QuizSession");
            _context = context;
        }

        public Quiz Quiz { get; set; } = default!;

        [BindProperty]
        public string StartToken { get; set; } = string.Empty;

        [BindProperty]
        public Dictionary<string, string> SelectedOptions { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string> TextAnswers { get; set; } = new();

        public double RemainingSeconds { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null)
            {
                return Forbid();
            }

            // Check if student has already submitted this quiz
            var existingResult = await _context.QuizResults.FirstOrDefaultAsync(qr => qr.QuizId == id && qr.StudentId == studentId.Value);
            if (existingResult != null)
            {
                TempData["ErrorMessage"] = $"Bạn đã hoàn thành bài test này trước đó. Điểm đạt được: {existingResult.Score:F2} điểm.";
                return RedirectToPage("/Quizzes/Available");
            }

            var quiz = await _quizService.GetPublishedQuizForStudentAsync(id, studentId.Value);
            if (quiz == null)
            {
                return NotFound();
            }

            Quiz = quiz;

            if (Quiz.QuizQuestions == null || !Quiz.QuizQuestions.Any())
            {
                TempData["ErrorMessage"] = "Bài test này chưa được cấu hình câu hỏi. Vui lòng liên hệ giáo viên.";
                return RedirectToPage("/Quizzes/Available");
            }

            // Generate start token if the quiz has a duration limit
            if (Quiz.DurationMinutes.HasValue && Quiz.DurationMinutes.Value > 0)
            {
                var cookieKey = $"Quiz_Start_{studentId.Value}_{id}";
                DateTime startTime;

                if (Request.Cookies.TryGetValue(cookieKey, out var existingCookieValue) && !string.IsNullOrEmpty(existingCookieValue))
                {
                    try
                    {
                        var decryptedCookie = _protector.Unprotect(existingCookieValue);
                        var ticks = long.Parse(decryptedCookie);
                        startTime = new DateTime(ticks, DateTimeKind.Utc);
                    }
                    catch
                    {
                        startTime = DateTime.UtcNow;
                        var cookieValue = _protector.Protect(startTime.Ticks.ToString());
                        Response.Cookies.Append(cookieKey, cookieValue, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTimeOffset.UtcNow.AddMinutes(Quiz.DurationMinutes.Value + 15)
                        });
                    }
                }
                else
                {
                    startTime = DateTime.UtcNow;
                    var cookieValue = _protector.Protect(startTime.Ticks.ToString());
                    Response.Cookies.Append(cookieKey, cookieValue, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(Quiz.DurationMinutes.Value + 15)
                    });
                }

                var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                if (elapsedSeconds > Quiz.DurationMinutes.Value * 60)
                {
                    // Time has expired before they loaded the page (e.g. resumed after closing)
                    // Auto submit a blank attempt
                    Response.Cookies.Delete(cookieKey);
                    var submitResult = await _quizService.SubmitQuizAsync(id, studentId.Value, new Dictionary<int, int>(), new Dictionary<int, string>());
                    TempData["ErrorMessage"] = $"Thời gian làm bài của bạn đã hết. Bài làm đã được tự động nộp. Điểm đạt được: {submitResult.Score:F2} điểm.";
                    return RedirectToPage("/Quizzes/Available");
                }

                var tokenPayload = $"{studentId.Value}:{id}:{startTime.Ticks}";
                StartToken = _protector.Protect(tokenPayload);

                RemainingSeconds = Math.Max(0, (Quiz.DurationMinutes.Value * 60) - elapsedSeconds);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == null)
            {
                return Forbid();
            }

            var quiz = await _quizService.GetPublishedQuizForStudentAsync(id, studentId.Value);
            if (quiz == null)
            {
                return NotFound();
            }
            Quiz = quiz;
            var cookieKey = $"Quiz_Start_{studentId.Value}_{id}";

            // Validate time limit if DurationMinutes is specified
            if (Quiz.DurationMinutes.HasValue && Quiz.DurationMinutes.Value > 0)
            {

                if (string.IsNullOrEmpty(StartToken))
                {
                    Response.Cookies.Delete(cookieKey);
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin bắt đầu làm bài.";
                    return RedirectToPage(new { id });
                }

                try
                {
                    var decrypted = _protector.Unprotect(StartToken);
                    var parts = decrypted.Split(':');
                    if (parts.Length != 3)
                    {
                        throw new Exception("Invalid token format.");
                    }

                    var tokenStudentId = int.Parse(parts[0]);
                    var tokenQuizId = int.Parse(parts[1]);
                    var tokenTicks = long.Parse(parts[2]);
                    var startTime = new DateTime(tokenTicks, DateTimeKind.Utc);

                    if (tokenStudentId != studentId.Value || tokenQuizId != id)
                    {
                        Response.Cookies.Delete(cookieKey);
                        TempData["ErrorMessage"] = "Thông tin làm bài không hợp lệ.";
                        return RedirectToPage(new { id });
                    }

                    var timeElapsed = DateTime.UtcNow - startTime;
                    var maxAllowedTime = TimeSpan.FromMinutes(Quiz.DurationMinutes.Value).Add(TimeSpan.FromSeconds(30)); // 30s grace period

                    if (timeElapsed > maxAllowedTime)
                    {
                        Response.Cookies.Delete(cookieKey);
                        await _quizService.SubmitQuizAsync(id, studentId.Value, new Dictionary<int, int>(), new Dictionary<int, string>());
                        TempData["ErrorMessage"] = "Bài nộp bị từ chối do quá thời gian quy định. Lượt làm bài này được tính 0 điểm.";
                        return RedirectToPage("/Quizzes/Available");
                    }
                }
                catch (Exception)
                {
                    Response.Cookies.Delete(cookieKey);
                    TempData["ErrorMessage"] = "Mã xác thực thời gian làm bài không hợp lệ hoặc đã hết hạn.";
                    return RedirectToPage(new { id });
                }
            }
            if (Quiz.QuizQuestions == null || !Quiz.QuizQuestions.Any())
            {
                TempData["ErrorMessage"] = "Bài test không có câu hỏi để chấm.";
                return RedirectToPage("/Quizzes/Available");
            }

            var selectedOptionsInt = SelectedOptions
                .Where(kvp => int.TryParse(kvp.Key, out _) && int.TryParse(kvp.Value, out _))
                .ToDictionary(kvp => int.Parse(kvp.Key), kvp => int.Parse(kvp.Value));

            var textAnswersInt = TextAnswers
                .Where(kvp => int.TryParse(kvp.Key, out _))
                .ToDictionary(kvp => int.Parse(kvp.Key), kvp => kvp.Value ?? string.Empty);

            var result = await _quizService.SubmitQuizAsync(id, studentId.Value, selectedOptionsInt, textAnswersInt);
            
            // Delete the quiz start cookie regardless of success to clean up
            Response.Cookies.Delete(cookieKey);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToPage(new { id });
            }

            TempData["SuccessMessage"] = $"{result.Message} Điểm: {result.Score}.";
            return RedirectToPage("/Quizzes/Available");
        }

        private int? GetCurrentStudentId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var studentId) ? studentId : null;
        }
    }
}
