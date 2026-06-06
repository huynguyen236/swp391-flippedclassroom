using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface IFileStorageService
    {
        // Lưu tệp tải lên vào thư mục con trong wwwroot, trả về đường dẫn tương đối để lưu DB.
        // Trả về null nếu tệp rỗng hoặc lỗi.
        Task<string?> SaveUploadAsync(IFormFile file, string subFolder);
    }
}
