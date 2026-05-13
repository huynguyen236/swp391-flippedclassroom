using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Net;

namespace Flipped_Classroom.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly FlippedClassroomContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(FlippedClassroomContext db, IConfiguration config, ILogger<AuthService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Sinh token ngẫu nhiên 32 bytes, encode base64
        /// </summary>
        public string GeneratePasswordResetToken()
        {
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }

        /// <summary>
        /// Tìm user theo email, sinh token, lưu vào DB
        /// Trả về token nếu thành công, null nếu không tìm thấy user
        /// </summary>
        public async Task<string?> GenerateResetTokenAsync(string email)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.Trim());
                if (user == null)
                {
                    _logger.LogWarning("User không tìm thấy với email: {Email}", email);
                    return null;
                }

                // Sinh token mới
                var token = GeneratePasswordResetToken();
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpiry = DateTime.Now.AddHours(1); // Token hết hạn sau 1 giờ

                await _db.SaveChangesAsync();
                _logger.LogInformation("Sinh token reset password cho user: {Email}", email);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi sinh token reset password");
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra token hợp lệ (tồn tại, chưa hết hạn)
        /// </summary>
        public async Task<User?> ValidateResetTokenAsync(string token)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
                
                if (user == null)
                {
                    _logger.LogWarning("Token không hợp lệ");
                    return null;
                }

                // Kiểm tra hết hạn
                if (user.PasswordResetTokenExpiry < DateTime.Now)
                {
                    _logger.LogWarning("Token hết hạn cho user: {Email}", user.Email);
                    return null;
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi validate token");
                return null;
            }
        }

        /// <summary>
        /// Reset mật khẩu mới
        /// </summary>
        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                var user = await ValidateResetTokenAsync(token);
                if (user == null)
                    return false;

                // Lưu password mới không hash
                user.Password = newPassword;
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;

                await _db.SaveChangesAsync();
                _logger.LogInformation("Reset mật khẩu thành công cho user: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi reset mật khẩu");
                return false;
            }
        }

        /// <summary>
        /// Hash mật khẩu bằng SHA256
        /// </summary>
        private static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Gửi email reset password (cần cấu hình SMTP)
        /// </summary>
        public async Task<bool> SendResetEmailAsync(string email, string resetToken)
        {
            try
            {
                // Lấy cấu hình SMTP từ appsettings.json
                var smtpHost = _config["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var smtpUser = _config["EmailSettings:SmtpUser"];
                var smtpPassword = _config["EmailSettings:SmtpPassword"];
                var fromEmail = _config["EmailSettings:FromEmail"];
                var appUrl = _config["AppSettings:AppUrl"] ?? "http://localhost:5036";

                if (string.IsNullOrEmpty(smtpHost))
                {
                    _logger.LogWarning("SMTP chưa cấu hình");
                    return false;
                }

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(smtpUser, smtpPassword);

                    var resetLink = $"{appUrl}/Authentication/ResetPassword?token={Uri.EscapeDataString(resetToken)}";
                    var message = new MailMessage(fromEmail, email)
                    {
                        Subject = "Đặt lại mật khẩu - Convenient Store Management",
                        Body = $@"
                            <h2>Đặt lại mật khẩu</h2>
                            <p>Bạn đã yêu cầu đặt lại mật khẩu. Nhấp vào liên kết bên dưới để tiếp tục:</p>
                            <p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>
                            <p>Liên kết này sẽ hết hạn trong 1 giờ.</p>
                            <p>Nếu bạn không yêu cầu điều này, vui lòng bỏ qua email này.</p>
                        ",
                        IsBodyHtml = true
                    };

                    await client.SendMailAsync(message);
                    _logger.LogInformation("Gửi email reset password cho: {Email}", email);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi email");
                return false;
            }
        }
    }
}
