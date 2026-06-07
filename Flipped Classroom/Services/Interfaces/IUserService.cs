using System.Threading.Tasks;
using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    /// <summary>
    /// Xử lý các thao tác liên quan đến thông tin người dùng và tài khoản cá nhân.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Lấy thông tin người dùng theo Id.
        /// </summary>
        Task<User?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Cập nhật thông tin cá nhân (Họ tên, số điện thoại, ảnh đại diện).
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(
            int userId,
            string firstName,
            string lastName,
            string? phoneNumber,
            string? avatarUrl
        );

        /// <summary>
        /// Thay đổi mật khẩu người dùng.
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword
        );
    }
}
