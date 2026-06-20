using System.Security.Claims;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.Quizzes
{
    [Authorize(Roles = "Student")]
    public class TakeModel : PageModel
    {
        private readonly IQuizService _quizService;
        private readonly IDataProtector _protector;

        public TakeModel(IQuizService quizService, IDataProtectionProvider protectionProvider)
        {
            _quizService = quizService;
            _protector = protectionProvider.CreateProtector("FlippedClassroom.QuizSession");
        }

        public Quiz Quiz { get; set; } = default!;

        [BindProperty]
        public string StartToken { get; set; } = string.Empty;

        [BindProperty]
        public Dictionary<int, int> SelectedOptions { get; set; } = new();

        [BindProperty]
        public Dictionary<int, string> TextAnswers { get; set; } = new();

        public double RemainingSeconds { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
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

                var tokenPayload = $"{studentId.Value}:{id}:{startTime.Ticks}";
                StartToken = _protector.Protect(tokenPayload);

                var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
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

            // Validate time limit if DurationMinutes is specified
            if (Quiz.DurationMinutes.HasValue && Quiz.DurationMinutes.Value > 0)
            {
                if (string.IsNullOrEmpty(StartToken))
                {
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
                        TempData["ErrorMessage"] = "Thông tin làm bài không hợp lệ.";
                        return RedirectToPage(new { id });
                    }

                    var timeElapsed = DateTime.UtcNow - startTime;
                    var maxAllowedTime = TimeSpan.FromMinutes(Quiz.DurationMinutes.Value).Add(TimeSpan.FromSeconds(30)); // 30s grace period

                    if (timeElapsed > maxAllowedTime)
                    {
                        TempData["ErrorMessage"] = "Bài nộp bị từ chối do quá thời gian quy định.";
                        return RedirectToPage(new { id });
                    }
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "Mã xác thực thời gian làm bài không hợp lệ hoặc đã hết hạn.";
                    return RedirectToPage(new { id });
                }
            }

            var result = await _quizService.SubmitQuizAsync(id, studentId.Value, SelectedOptions, TextAnswers);
            
            // Delete the quiz start cookie regardless of success to clean up
            var cookieKey = $"Quiz_Start_{studentId.Value}_{id}";
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
