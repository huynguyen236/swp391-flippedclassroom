using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Flipped_Classroom.Services.Implementation;

public class WordExcelImportService : IWordExcelImportService
{
    public async Task<(List<Vocabulary> ValidVocabularies, List<string> Errors)> ParseExcelAsync(IFormFile file, int nodeId)
    {
        var validVocabularies = new List<Vocabulary>();
        var errors = new List<string>();

        // Đăng ký provider mã hóa để ExcelDataReader hỗ trợ các định dạng cũ
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var fileExt = Path.GetExtension(file.FileName).ToLower();
            using var reader = fileExt == ".csv"
                ? ExcelReaderFactory.CreateCsvReader(stream)
                : ExcelReaderFactory.CreateReader(stream);

            int rowIndex = 1;
            bool isHeader = true;

            while (reader.Read())
            {
                if (isHeader)
                {
                    isHeader = false; // Bỏ qua dòng đầu tiên chứa tiêu đề
                    rowIndex++;
                    continue;
                }

                // Đọc các giá trị trong dòng
                var word = reader.GetValue(0)?.ToString()?.Trim();
                var hiragana = reader.GetValue(1)?.ToString()?.Trim();
                var romaji = reader.GetValue(2)?.ToString()?.Trim();
                var meaning = reader.GetValue(3)?.ToString()?.Trim();
                var difficultyStr = reader.GetValue(4)?.ToString()?.Trim();

                // Nếu tất cả các cột đều trống thì bỏ qua dòng đó
                if (string.IsNullOrEmpty(word) && string.IsNullOrEmpty(hiragana) && string.IsNullOrEmpty(romaji) && 
                    string.IsNullOrEmpty(meaning) && string.IsNullOrEmpty(difficultyStr))
                {
                    continue;
                }

                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(hiragana) || string.IsNullOrEmpty(meaning))
                {
                    errors.Add($"Dòng {rowIndex}: Thiếu thông tin bắt buộc (Từ gốc, Cách đọc hoặc Ý nghĩa không được để trống).");
                    rowIndex++;
                    continue;
                }

                // Xử lý độ khó
                int difficulty = 5; // Mặc định là N5 (DifficultyLevel = 5)
                if (!string.IsNullOrEmpty(difficultyStr))
                {
                    var cleanDiff = difficultyStr.Replace("N", "", StringComparison.OrdinalIgnoreCase).Trim();
                    if (int.TryParse(cleanDiff, out int parsedDiff) && parsedDiff >= 1 && parsedDiff <= 5)
                    {
                        difficulty = parsedDiff;
                    }
                    else
                    {
                        errors.Add($"Dòng {rowIndex}: Giá trị độ khó '{difficultyStr}' không hợp lệ (Phải là số 1-5 hoặc N1-N5).");
                        rowIndex++;
                        continue;
                    }
                }

                validVocabularies.Add(new Vocabulary
                {
                    NodeId = nodeId,
                    Word = word,
                    Hiragana = hiragana,
                    Romaji = romaji,
                    Meaning = meaning,
                    DifficultyLevel = difficulty
                });

                rowIndex++;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Lỗi khi đọc file Excel/CSV: {ex.Message}");
        }

        return (validVocabularies, errors);
    }

    public async Task<(List<Vocabulary> ValidVocabularies, List<string> Errors)> ParseWordAsync(IFormFile file, int nodeId)
    {
        var validVocabularies = new List<Vocabulary>();
        var errors = new List<string>();

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var wordDoc = WordprocessingDocument.Open(stream, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;
            var table = body?.Elements<Table>().FirstOrDefault();

            if (table == null)
            {
                errors.Add("Không tìm thấy bảng danh sách từ vựng nào trong file Word (.docx).");
                return (validVocabularies, errors);
            }

            var rows = table.Elements<TableRow>().ToList();
            if (rows.Count <= 1)
            {
                errors.Add("Bảng từ vựng trong Word không có dòng dữ liệu nào.");
                return (validVocabularies, errors);
            }

            for (int i = 1; i < rows.Count; i++) // Bỏ qua dòng tiêu đề i = 0
            {
                var cells = rows[i].Elements<TableCell>().ToList();
                if (cells.Count < 4)
                {
                    errors.Add($"Dòng {i + 1} (trong bảng Word): Bảng phải có tối thiểu 4 cột (Từ gốc, Cách đọc, Romaji, Ý nghĩa).");
                    continue;
                }

                var word = cells[0].InnerText?.Trim();
                var hiragana = cells[1].InnerText?.Trim();
                var romaji = cells[2].InnerText?.Trim();
                var meaning = cells[3].InnerText?.Trim();
                var difficultyStr = cells.Count >= 5 ? cells[4].InnerText?.Trim() : "";

                // Bỏ qua dòng trống
                if (string.IsNullOrEmpty(word) && string.IsNullOrEmpty(hiragana) && string.IsNullOrEmpty(romaji) && 
                    string.IsNullOrEmpty(meaning) && string.IsNullOrEmpty(difficultyStr))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(hiragana) || string.IsNullOrEmpty(meaning))
                {
                    errors.Add($"Dòng {i + 1} (trong bảng Word): Thiếu thông tin bắt buộc (Từ gốc, Cách đọc, Ý nghĩa).");
                    continue;
                }

                int difficulty = 5; // Mặc định N5
                if (!string.IsNullOrEmpty(difficultyStr))
                {
                    var cleanDiff = difficultyStr.Replace("N", "", StringComparison.OrdinalIgnoreCase).Trim();
                    if (int.TryParse(cleanDiff, out int parsedDiff) && parsedDiff >= 1 && parsedDiff <= 5)
                    {
                        difficulty = parsedDiff;
                    }
                    else
                    {
                        errors.Add($"Dòng {i + 1} (trong bảng Word): Giá trị độ khó '{difficultyStr}' không hợp lệ (Phải là số 1-5 hoặc N1-N5).");
                        continue;
                    }
                }

                validVocabularies.Add(new Vocabulary
                {
                    NodeId = nodeId,
                    Word = word,
                    Hiragana = hiragana,
                    Romaji = romaji,
                    Meaning = meaning,
                    DifficultyLevel = difficulty
                });
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Lỗi khi đọc file Word: {ex.Message}");
        }

        return (validVocabularies, errors);
    }

    public byte[] GenerateCsvTemplate()
    {
        using var stream = new MemoryStream();
        // Ghi kèm UTF-8 BOM để Excel tự nhận diện và hiển thị đúng tiếng Việt, tiếng Nhật
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        stream.Write(bom, 0, bom.Length);

        using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(true), 1024, true))
        {
            writer.WriteLine("Từ gốc (Kanji/Kana),Cách đọc (Hiragana),Romaji,Ý nghĩa,Độ khó (N1-N5)");
            writer.WriteLine("食べる,たべる,taberu,Ăn,5");
            writer.WriteLine("飲む,のむ,nomu,Uống,5");
            writer.WriteLine("学生,がくせい,gakusei,Học sinh/Sinh viên,5");
        }

        return stream.ToArray();
    }

    public byte[] GenerateWordTemplate()
    {
        using var stream = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();
            mainPart.Document.Append(body);

            // Thêm tiêu đề tài liệu mẫu
            var titlePara = new Paragraph(new Run(new Text("FILE MẪU NHẬP TỪ VỰNG HÀNG LOẠT (VUI LÒNG ĐIỀN VÀO BẢNG DƯỚI ĐÂY)"))
            {
                RunProperties = new RunProperties(new Bold(), new FontSize { Val = "24" })
            });
            body.Append(titlePara);

            // Thêm một đoạn trống
            body.Append(new Paragraph(new Run(new Text(""))));

            // Tạo bảng
            var table = new Table();

            // Cấu hình viền cho bảng
            var tableBorders = new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "E0E0E0" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "E0E0E0" }
            );
            table.AppendChild(new TableProperties(tableBorders));

            // Dòng tiêu đề
            var headerRow = new TableRow();
            headerRow.Append(CreateCell("Từ gốc (Kanji/Kana)", true));
            headerRow.Append(CreateCell("Cách đọc (Hiragana)", true));
            headerRow.Append(CreateCell("Romaji", true));
            headerRow.Append(CreateCell("Ý nghĩa", true));
            headerRow.Append(CreateCell("Độ khó (N1-N5)", true));
            table.Append(headerRow);

            // Dòng mẫu 1
            var row1 = new TableRow();
            row1.Append(CreateCell("食べる"));
            row1.Append(CreateCell("たべる"));
            row1.Append(CreateCell("taberu"));
            row1.Append(CreateCell("Ăn"));
            row1.Append(CreateCell("5"));
            table.Append(row1);

            // Dòng mẫu 2
            var row2 = new TableRow();
            row2.Append(CreateCell("飲む"));
            row2.Append(CreateCell("のむ"));
            row2.Append(CreateCell("nomu"));
            row2.Append(CreateCell("Uống"));
            row2.Append(CreateCell("5"));
            table.Append(row2);

            body.Append(table);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private TableCell CreateCell(string text, bool isHeader = false)
    {
        var runProps = new RunProperties();
        if (isHeader)
        {
            runProps.Append(new Bold());
        }

        var run = new Run(new Text(text)) { RunProperties = runProps };
        var para = new Paragraph(run);
        return new TableCell(para);
    }
}
