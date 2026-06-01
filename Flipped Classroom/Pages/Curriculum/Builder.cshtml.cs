using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Curriculum = Flipped_Classroom.Models.Curriculum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Flipped_Classroom.Pages.Curriculums
{
    [Authorize(Roles = "Admin,Manager")]
    public class BuilderModel : PageModel
    {
        private readonly ICurriculumService _curriculumService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BuilderModel> _logger;

        public BuilderModel(ICurriculumService curriculumService, IWebHostEnvironment environment, ILogger<BuilderModel> logger)
        {
            _curriculumService = curriculumService;
            _environment = environment;
            _logger = logger;
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

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Curriculum = await _curriculumService.GetCurriculumByIdAsync(id);
            if (Curriculum == null)
            {
                return NotFound();
            }
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

            if (MaterialType == "YouTube")
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

                try
                {
                    var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "materials");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    var extension = Path.GetExtension(UploadedFile.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadDir, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await UploadedFile.CopyToAsync(fileStream);
                    }

                    url = $"/uploads/materials/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lưu tệp tin học liệu cho node: {NodeId}", NodeId);
                    TempData["ErrorMessage"] = "Lỗi trong quá trình lưu tệp tin lên máy chủ.";
                    return RedirectToPage(new { id });
                }
            }

            var material = new Material
            {
                NodeId = NodeId,
                Title = MaterialTitle.Trim(),
                MaterialType = MaterialType,
                Url = url
            };

            await _curriculumService.AddMaterialAsync(material);
            TempData["SuccessMessage"] = "Đêm học liệu thành công.";
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
    }
}
