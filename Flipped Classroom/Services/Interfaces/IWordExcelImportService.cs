using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Http;

namespace Flipped_Classroom.Services.Interfaces;

public interface IWordExcelImportService
{
    // Đọc danh sách từ vựng từ file Excel, trả về tuple gồm (Từ vựng hợp lệ, Danh sách lỗi)
    Task<(List<Vocabulary> ValidVocabularies, List<string> Errors)> ParseExcelAsync(IFormFile file, int nodeId);
    
    // Đọc danh sách từ vựng từ file Word, trả về tuple gồm (Từ vựng hợp lệ, Danh sách lỗi)
    Task<(List<Vocabulary> ValidVocabularies, List<string> Errors)> ParseWordAsync(IFormFile file, int nodeId);

    // Tạo nội dung file CSV mẫu để người dùng tải về
    byte[] GenerateCsvTemplate();

    // Tạo nội dung file Word mẫu để người dùng tải về
    byte[] GenerateWordTemplate();
}
