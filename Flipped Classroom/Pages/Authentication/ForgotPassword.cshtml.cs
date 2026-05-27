using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Flipped_Classroom.Services.Interfaces;

namespace Flipped_Classroom.Pages.Authentication
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(IAuthService authService, ILogger<ForgotPasswordModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public string? Email { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Hiển thị form quên mật khẩu
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Vui lòng nhập email";
                return Page();
            }

            try
            {
                // Tạo token reset
                var token = await _authService.GenerateResetTokenAsync(Email.Trim());
                if (token == null)
                {
                    // Không nêu rõ user không tồn tại (security best practice)
                    SuccessMessage = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được liên kết đặt lại mật khẩu trong vòng vài phút.";
                    _logger.LogWarning("Yêu cầu reset password cho email không tồn tại: {Email}", Email);
                    return Page();
                }

                // Gửi email
                var resetLink = Url.Page(
                    "/Authentication/ResetPassword",
                    pageHandler: null,
                    values: new { token },
                    protocol: Request.Scheme);

                if (string.IsNullOrWhiteSpace(resetLink))
                {
                    _logger.LogWarning("Không tạo được reset link cho email: {Email}", Email);
                    ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                    return Page();
                }

                var emailSent = await _authService.SendResetEmailAsync(Email.Trim(), resetLink);
                if (!emailSent)
                {
                    _logger.LogWarning("Gửi email thất bại cho: {Email}", Email);
                }

                SuccessMessage = "Liên kết đặt lại mật khẩu đã được gửi tới email của bạn. Vui lòng kiểm tra email.";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý quên mật khẩu");
                ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return Page();
            }
        }
    }
}
