using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Flipped_Classroom.Services.Interfaces;

namespace Flipped_Classroom.Pages.Authentication
{
    public class ResetPasswordModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(IAuthService authService, ILogger<ResetPasswordModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [BindProperty]
        public string? Token { get; set; }

        [BindProperty]
        public string? NewPassword { get; set; }

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorMessage = "Token không hợp lệ hoặc bị thiếu";
                _logger.LogWarning("Truy cập ResetPassword mà không có token");
                return Page();
            }

            // Kiểm tra token hợp lệ
            var user = await _authService.ValidateResetTokenAsync(token);
            if (user == null)
            {
                ErrorMessage = "Token không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu liên kết mới.";
                _logger.LogWarning("Token không hợp lệ: {Token}", token);
                return Page();
            }

            Token = token;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                ErrorMessage = "Token không hợp lệ";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu mới";
                return Page();
            }

            if (NewPassword.Length < 6)
            {
                ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự";
                return Page();
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Mật khẩu không khớp";
                return Page();
            }

            try
            {
                var result = await _authService.ResetPasswordAsync(Token, NewPassword);
                if (!result)
                {
                    ErrorMessage = "Token không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu liên kết mới.";
                    _logger.LogWarning("Reset password thất bại với token: {Token}", Token);
                    return Page();
                }

                SuccessMessage = "Mật khẩu của bạn đã được đặt lại thành công. Bạn có thể đăng nhập bây giờ.";
                _logger.LogInformation("Reset password thành công");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đặt lại mật khẩu");
                ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return Page();
            }
        }
    }
}
