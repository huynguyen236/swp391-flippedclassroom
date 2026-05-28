using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Flipped_Classroom.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly Swp391NihongoContext _db;
        private readonly ILogger<UserService> _logger;

        public UserService(Swp391NihongoContext db, ILogger<UserService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Lấy thông tin người dùng theo Id.
        /// </summary>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin người dùng với Id: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Cập nhật thông tin cá nhân.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(
            int userId, 
            string firstName, 
            string lastName, 
            string? phoneNumber, 
            string? avatarUrl)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return (false, "Người dùng không tồn tại trên hệ thống.");
                }

                // Cập nhật thông tin
                user.FirstName = firstName.Trim();
                user.LastName = lastName.Trim();
                user.PhoneNumber = phoneNumber?.Trim();

                if (!string.IsNullOrWhiteSpace(avatarUrl))
                {
                    user.AvatarUrl = avatarUrl;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("Cập nhật thông tin cá nhân thành công cho User ID: {UserId}", userId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật thông tin cá nhân cho User ID: {UserId}", userId);
                return (false, "Có lỗi xảy ra trong quá trình lưu thông tin. Vui lòng thử lại.");
            }
        }

        /// <summary>
        /// Thay đổi mật khẩu người dùng.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(
            int userId, 
            string currentPassword, 
            string newPassword)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return (false, "Người dùng không tồn tại.");
                }

                // Nếu tài khoản Google chưa thiết lập mật khẩu thì user.PasswordHash có thể là null
                if (user.PasswordHash == null)
                {
                    // Cho phép họ thiết lập mật khẩu trực tiếp mà không cần check currentPassword
                    user.PasswordHash = HashPassword(newPassword);
                }
                else
                {
                    // Check mật khẩu hiện tại
                    if (HashPassword(currentPassword) != user.PasswordHash)
                    {
                        return (false, "Mật khẩu hiện tại không chính xác.");
                    }

                    user.PasswordHash = HashPassword(newPassword);
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("Đổi mật khẩu thành công cho User ID: {UserId}", userId);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thay đổi mật khẩu cho User ID: {UserId}", userId);
                return (false, "Có lỗi xảy ra trong quá trình đổi mật khẩu. Vui lòng thử lại.");
            }
        }

        /// <summary>
        /// Hash mật khẩu bằng SHA256 tương thích với AuthService
        /// </summary>
        private static string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
