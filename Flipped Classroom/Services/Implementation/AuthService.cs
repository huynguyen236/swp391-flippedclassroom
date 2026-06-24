using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2.Flows;

namespace Flipped_Classroom.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly Swp391NihongoContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(Swp391NihongoContext db, IConfiguration config, ILogger<AuthService> logger)
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

                user.PasswordHash = HashPassword(newPassword);
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
        public string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(
            string firstName,
            string lastName,
            string email,
            string username,
            string password)
        {
            try
            {
                var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (existingUser != null)
                {
                    return (false, "Username is already taken.");
                }

                var user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Username = username,
                    PasswordHash = HashPassword(password),
                    Role = "Student",
                    CreatedAt = DateTime.Now
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đăng ký tài khoản");
                return (false, "Có lỗi xảy ra. Vui lòng thử lại sau.");
            }
        }

        public async Task<(User? User, string? ErrorMessage)> AuthenticateAsync(string username, string password)
        { 
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return (null, "Wrong Username or Password.");
            }

            if (user.PasswordHash == null)
            {
                return (null, "This account uses Google Sign-In. Please use the \"Continue with Google\" button.");
            }

            if (HashPassword(password) != user.PasswordHash)
            {
                return (null, "Wrong Username or Password.");
            }
            if (user.IsActive == false)
            {
                return (null, "Your account has been deactivated. Please contact the administrator.");
            }

            // Kiểm tra tài khoản có bị vô hiệu hóa không
            if (user.IsActive == false)
            {
                return (null, "Your account has been deactivated. Please contact the administrator.");
            }

            return (user, null);
        }

    
        public async Task<bool> SendResetEmailAsync(string email, string resetLink)
        {
            var subject = "NihongoFlipedClassroom";
            var bodyHtml = $@"
<h2>Đặt lại mật khẩu</h2>
<p>Bạn đã yêu cầu đặt lại mật khẩu. Nhấp vào liên kết bên dưới để tiếp tục:</p>
<p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>
<p>Liên kết này sẽ hết hạn trong 1 giờ.</p>
<p>Nếu bạn không yêu cầu điều này, vui lòng bỏ qua email này.</p>";

            return await SendEmailAsync(email, subject, bodyHtml);
        }

        public async Task<bool> SendEmailAsync(string email, string subject, string bodyHtml)
        {
            try
            {
                var fromEmail = _config["EmailSettings:GmailApi:FromEmail"];

                if (string.IsNullOrWhiteSpace(fromEmail))
                {
                    _logger.LogWarning("Gmail API chưa cấu hình");
                    return false;
                }

                var gmailService = await CreateGmailServiceAsync(fromEmail);
                var raw = BuildRawMimeMessage(fromEmail, email, subject, bodyHtml);
                var message = new Message { Raw = raw };

                await gmailService.Users.Messages.Send(message, "me").ExecuteAsync();
                _logger.LogInformation("Gửi email thành công cho: {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi email bằng Gmail API");
                return false;
            }
        }

        private async Task<GmailService> CreateGmailServiceAsync(string userEmail)
        {
            var clientId = _config["EmailSettings:GmailApi:ClientId"];
            var clientSecret = _config["EmailSettings:GmailApi:ClientSecret"];
            var refreshToken = _config["EmailSettings:GmailApi:RefreshToken"];

            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(clientSecret) ||
                string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new InvalidOperationException("Thiếu cấu hình Gmail API (ClientId/ClientSecret/RefreshToken).");
            }

            var token = new TokenResponse { RefreshToken = refreshToken };
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                Scopes = new[] { GmailService.Scope.GmailSend }
            });

            var credential = new UserCredential(flow, userEmail, token);
            await credential.RefreshTokenAsync(CancellationToken.None);

            return new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "FlippedClassroom"
            });
        }

        private static string BuildRawMimeMessage(string from, string to, string subject, string htmlBody)
        {
            var mime = new StringBuilder()
                .AppendLine($"From: {from}")
                .AppendLine($"To: {to}")
                .AppendLine($"Subject: {subject}")
                .AppendLine("MIME-Version: 1.0")
                .AppendLine("Content-Type: text/html; charset=utf-8")
                .AppendLine()
                .AppendLine(htmlBody)
                .ToString();

            var bytes = Encoding.UTF8.GetBytes(mime);
            return Base64UrlEncode(bytes);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
