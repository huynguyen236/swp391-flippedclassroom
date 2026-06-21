using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface IFileStorageService
    {
        // Giới hạn dung lượng tối đa cho một tệp học liệu tải lên (200 MB).
        const long MaxUploadBytes = 200L * 1024 * 1024;

        // Lưu tệp tải lên vào thư mục con trong wwwroot, trả về đường dẫn tương đối để lưu DB.
        // Trả về null nếu tệp rỗng hoặc lỗi.
        Task<string?> SaveUploadAsync(IFormFile file, string subFolder);
    }
}
