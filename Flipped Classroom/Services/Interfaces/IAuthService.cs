using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    /// <summary>
    /// Xử lý các thao tác liên quan đến xác thực: reset password, gửi email, validate token
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Sinh token ngẫu nhiên cho reset password
        /// </summary>
        string GeneratePasswordResetToken();

        /// <summary>
        /// Tìm user theo email và sinh token reset password, lưu vào DB
        /// Trả về token nếu thành công, null nếu không tìm thấy user
        /// </summary>
        Task<string?> GenerateResetTokenAsync(string email);

        /// <summary>
        /// Kiểm tra token hợp lệ (tồn tại, chưa hết hạn)
        /// </summary>
        Task<User?> ValidateResetTokenAsync(string token);

        /// <summary>
        /// Reset mật khẩu mới cho user
        /// </summary>
        Task<bool> ResetPasswordAsync(string token, string newPassword);

        /// <summary>
        /// Gửi email reset password
        /// </summary>
        Task<bool> SendResetEmailAsync(string email, string resetLink);

        /// <summary>
        /// Đăng ký người dùng mới
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> RegisterAsync(
            string firstName,
            string lastName,
            string email,
            string username,
            string password);

        /// <summary>
        /// Xác thực đăng nhập
        /// </summary>
        Task<(User? User, string? ErrorMessage)> AuthenticateAsync(string username, string password);
    }
}
