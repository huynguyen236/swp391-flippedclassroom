using System;
using System.Collections.Generic;
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

        public VocabulariesModel(IVocabularyService vocabService, Swp391NihongoContext db)
        {
            _vocabService = vocabService;
            _db = db;
        }

        public Node Node { get; set; } = null!;
        public List<Vocabulary> Vocabularies { get; set; } = new();

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
    }
}
