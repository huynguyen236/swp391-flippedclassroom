using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.Curriculums
{
    [Authorize(Roles = "Admin,Manager")]
    public class VocabulariesModel : PageModel
    {
        private readonly IVocabularyService _vocabService;
        private readonly Swp391NihongoContext _db;
        private readonly IWordExcelImportService _importService;

        public VocabulariesModel(IVocabularyService vocabService, Swp391NihongoContext db, IWordExcelImportService importService)
        {
            _vocabService = vocabService;
            _db = db;
            _importService = importService;
        }

        public Node Node { get; set; } = null!;
        public List<Vocabulary> Vocabularies { get; set; } = new();

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        [BindProperty]
        public string Word { get; set; } = string.Empty;

        [BindProperty]
        public string Hiragana { get; set; } = string.Empty;

        [BindProperty]
        public string Meaning { get; set; } = string.Empty;

        [BindProperty]
        public string? Romaji { get; set; }

        [BindProperty]
        public int DifficultyLevel { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync(int nodeId)
        {
            var node = await _db.Nodes
                .Include(n => n.Curriculum)
                .FirstOrDefaultAsync(n => n.Id == nodeId);

            if (node == null)
            {
                return NotFound();
            }

            Node = node;
            Vocabularies = await _vocabService.GetVocabulariesByNodeAsync(nodeId);

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(int nodeId)
        {
            if (string.IsNullOrWhiteSpace(Word) || string.IsNullOrWhiteSpace(Hiragana) || string.IsNullOrWhiteSpace(Meaning))
            {
                TempData["ErrorMessage"] = "Các trường Từ gốc, Cách đọc, và Ý nghĩa không được để trống.";
                return RedirectToPage(new { nodeId });
            }

            var vocab = new Vocabulary
            {
                NodeId = nodeId,
                Word = Word.Trim(),
                Hiragana = Hiragana.Trim(),
                Meaning = Meaning.Trim(),
                Romaji = Romaji?.Trim(),
                DifficultyLevel = DifficultyLevel
            };

            await _vocabService.CreateVocabularyAsync(vocab);
            TempData["SuccessMessage"] = "Đã thêm từ vựng thành công!";
            return RedirectToPage(new { nodeId });
        }

        public async Task<IActionResult> OnPostUpdateAsync(int nodeId, int vocabId)
        {
            if (string.IsNullOrWhiteSpace(Word) || string.IsNullOrWhiteSpace(Hiragana) || string.IsNullOrWhiteSpace(Meaning))
            {
                TempData["ErrorMessage"] = "Các trường Từ gốc, Cách đọc, và Ý nghĩa không được để trống.";
                return RedirectToPage(new { nodeId });
            }

            var vocab = await _vocabService.GetVocabularyByIdAsync(vocabId);
            if (vocab == null)
            {
                return NotFound();
            }

            vocab.Word = Word.Trim();
            vocab.Hiragana = Hiragana.Trim();
            vocab.Meaning = Meaning.Trim();
            vocab.Romaji = Romaji?.Trim();
            vocab.DifficultyLevel = DifficultyLevel;

            await _vocabService.UpdateVocabularyAsync(vocab);
            TempData["SuccessMessage"] = "Cập nhật từ vựng thành công!";
            return RedirectToPage(new { nodeId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int nodeId, int vocabId)
        {
            var success = await _vocabService.DeleteVocabularyAsync(vocabId);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã xóa từ vựng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy từ vựng để xóa.";
            }
            return RedirectToPage(new { nodeId });
        }

        public async Task<IActionResult> OnPostImportFileAsync(int nodeId)
        {
            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn một file Excel/CSV (.xlsx, .xls, .csv) hoặc Word (.docx) hợp lệ.";
                return RedirectToPage(new { nodeId });
            }

            var extension = Path.GetExtension(UploadedFile.FileName).ToLower();
            List<Vocabulary> validVocabularies;
            List<string> errors;

            if (extension == ".xlsx" || extension == ".xls" || extension == ".csv")
            {
                (validVocabularies, errors) = await _importService.ParseExcelAsync(UploadedFile, nodeId);
            }
            else if (extension == ".docx")
            {
                (validVocabularies, errors) = await _importService.ParseWordAsync(UploadedFile, nodeId);
            }
            else
            {
                TempData["ErrorMessage"] = "Định dạng file không được hỗ trợ. Vui lòng chỉ tải lên file Excel (.xlsx, .xls, .csv) hoặc Word (.docx).";
                return RedirectToPage(new { nodeId });
            }

            if (errors.Count > 0)
            {
                TempData["ErrorMessage"] = "Nhập dữ liệu thất bại! Các lỗi phát hiện:\n" + string.Join("\n", errors.Take(5));
                if (errors.Count > 5)
                {
                    TempData["ErrorMessage"] += $"\n... và {errors.Count - 5} lỗi khác.";
                }
                return RedirectToPage(new { nodeId });
            }

            if (validVocabularies.Count == 0)
            {
                TempData["ErrorMessage"] = "Không tìm thấy từ vựng nào hợp lệ trong file tải lên.";
                return RedirectToPage(new { nodeId });
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Tránh chèn từ trùng trong cùng một bài học (Node) dựa trên Word
                var existingWords = await _db.Vocabularies
                    .Where(v => v.NodeId == nodeId)
                    .Select(v => v.Word)
                    .ToListAsync();

                var toInsert = validVocabularies
                    .Where(v => !existingWords.Contains(v.Word))
                    .ToList();

                if (toInsert.Count > 0)
                {
                    await _db.Vocabularies.AddRangeAsync(toInsert);
                    await _db.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Đã nhập thành công {toInsert.Count} từ vựng mới! (Bỏ qua {validVocabularies.Count - toInsert.Count} từ trùng lặp).";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Lỗi hệ thống khi lưu vào cơ sở dữ liệu: {ex.Message}";
            }

            return RedirectToPage(new { nodeId });
        }

        public IActionResult OnGetDownloadTemplate(string type)
        {
            if (string.Equals(type, "excel", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = _importService.GenerateCsvTemplate();
                return File(bytes, "text/csv; charset=utf-8", "vocab_template.csv");
            }
            else if (string.Equals(type, "word", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = _importService.GenerateWordTemplate();
                return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "vocab_template.docx");
            }

            return BadRequest("Loại file template không hợp lệ.");
        }
    }
}
