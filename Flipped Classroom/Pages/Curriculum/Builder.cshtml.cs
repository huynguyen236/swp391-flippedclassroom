using System.Linq;
using System.Threading.Tasks;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Curriculum = Flipped_Classroom.Models.Curriculum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.Curriculums
{
    [Authorize(Roles = "Admin,Manager")]
    public class BuilderModel : PageModel
    {
        private readonly ICurriculumService _curriculumService;
        private readonly IFileStorageService _fileStorage;
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _db;

        public BuilderModel(ICurriculumService curriculumService, IFileStorageService fileStorage, Flipped_Classroom.Data.Swp391NihongoContext db)
        {
            _curriculumService = curriculumService;
            _fileStorage = fileStorage;
            _db = db;
        }

        public Flipped_Classroom.Models.Curriculum Curriculum { get; set; } = null!;

        [BindProperty]
        public string NodeTitle { get; set; } = string.Empty;

        [BindProperty]
        public string? NodeDescription { get; set; }

        [BindProperty]
        public int? ParentNodeId { get; set; }

        [BindProperty]
        public int NodeId { get; set; }

        [BindProperty]
        public string MaterialTitle { get; set; } = string.Empty;

        [BindProperty]
        public string MaterialType { get; set; } = "YouTube"; // YouTube or LocalFile

        [BindProperty]
        public string? ExternalUrl { get; set; }

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        // Dành cho MaterialType == "speech" (luyện nói)
        [BindProperty]
        public string? SpeechTargetText { get; set; }

        [BindProperty]
        public string? SpeechMeaning { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var curriculum = await _curriculumService.GetCurriculumByIdAsync(id);
            if (curriculum == null)
            {
                return NotFound();
            }
            Curriculum = curriculum;
            return Page();
        }

        public async Task<IActionResult> OnPostCreateNodeAsync(int id)
        {
            if (string.IsNullOrWhiteSpace(NodeTitle))
            {
                TempData["ErrorMessage"] = "Tiêu đề bài học/chương không được để trống.";
                return RedirectToPage(new { id });
            }

            var node = new Node
            {
                Title = NodeTitle.Trim(),
                Description = NodeDescription?.Trim(),
                CurriculumId = id,
                ParentNodeId = ParentNodeId,
                IsActive = true,
                Status = "Draft"
            };

            await _curriculumService.CreateNodeAsync(node);
            TempData["SuccessMessage"] = "Đã tạo bài học/chương mới thành công.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAddMaterialAsync(int id)
        {
            if (string.IsNullOrWhiteSpace(MaterialTitle))
            {
                TempData["ErrorMessage"] = "Tiêu đề học liệu không được để trống.";
                return RedirectToPage(new { id });
            }

            string url = string.Empty;
            string? speechTargetText = null;
            string? speechMeaning = null;

            if (MaterialType == "speech")
            {
                if (string.IsNullOrWhiteSpace(SpeechTargetText))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập câu mẫu tiếng Nhật để luyện nói.";
                    return RedirectToPage(new { id });
                }
                speechTargetText = SpeechTargetText.Trim();
                speechMeaning = SpeechMeaning?.Trim();
                // url để rỗng — material loại speech không có file/đường dẫn
            }
            else if (MaterialType == "YouTube")
            {
                if (string.IsNullOrWhiteSpace(ExternalUrl))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập đường dẫn YouTube.";
                    return RedirectToPage(new { id });
                }
                url = ExternalUrl.Trim();
            }
            else if (MaterialType == "LocalFile")
            {
                if (UploadedFile == null || UploadedFile.Length == 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn tệp tin học liệu.";
                    return RedirectToPage(new { id });
                }

                if (UploadedFile.Length > IFileStorageService.MaxUploadBytes)
                {
                    var maxMb = IFileStorageService.MaxUploadBytes / (1024 * 1024);
                    TempData["ErrorMessage"] = $"Tệp tin quá lớn. Dung lượng tối đa cho phép là {maxMb} MB.";
                    return RedirectToPage(new { id });
                }

                var savedUrl = await _fileStorage.SaveUploadAsync(UploadedFile, "materials");
                if (savedUrl == null)
                {
                    TempData["ErrorMessage"] = "Lỗi trong quá trình lưu tệp tin lên máy chủ.";
                    return RedirectToPage(new { id });
                }

                url = savedUrl;
            }

            var material = new Material
            {
                NodeId = NodeId,
                Title = MaterialTitle.Trim(),
                MaterialType = MaterialType,
                Url = url,
                SpeechTargetText = speechTargetText,
                SpeechMeaning = speechMeaning
            };

            await _curriculumService.AddMaterialAsync(material);
            TempData["SuccessMessage"] = "Thêm học liệu thành công.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteNodeAsync(int id, int nodeId)
        {
            var success = await _curriculumService.DeleteNodeAsync(nodeId);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã xóa bài học/chương thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa bài học/chương.";
            }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteMaterialAsync(int id, int materialId)
        {
            var success = await _curriculumService.DeleteMaterialAsync(materialId);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã xóa học liệu thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa học liệu.";
            }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteQuizAsync(int id, int quizId)
        {
            var quiz = await _db.Quizzes.FindAsync(quizId);
            if (quiz != null)
            {
                _db.Quizzes.Remove(quiz);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa bài test thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài test để xóa.";
            }
            return RedirectToPage(new { id });
        }
    }
}
