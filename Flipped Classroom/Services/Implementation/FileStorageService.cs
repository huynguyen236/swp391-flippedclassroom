using System;
using System.IO;
using System.Threading.Tasks;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Flipped_Classroom.Services.Implementations
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string?> SaveUploadAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            try
            {
                var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", subFolder);
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var extension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Đường dẫn tương đối phục vụ web (luôn dùng dấu '/')
                return $"/uploads/{subFolder}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu tệp tải lên vào thư mục {SubFolder}", subFolder);
                return null;
            }
        }
    }
}
