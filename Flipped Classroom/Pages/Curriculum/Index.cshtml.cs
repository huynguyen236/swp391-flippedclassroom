using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Curriculum = Flipped_Classroom.Models.Curriculum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages.Curriculums
{
    [Authorize(Roles = "Admin,Manager")]
    public class IndexModel : PageModel
    {
        private readonly ICurriculumService _curriculumService;

        public IndexModel(ICurriculumService curriculumService)
        {
            _curriculumService = curriculumService;
        }

        public List<Flipped_Classroom.Models.Curriculum> Curriculums { get; set; } = new List<Flipped_Classroom.Models.Curriculum>();

        [BindProperty]
        public string NewCurriculumName { get; set; } = string.Empty;

        [BindProperty]
        public string? NewDescription { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Curriculums = await _curriculumService.GetAllCurriculumsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCurriculumName))
            {
                ModelState.AddModelError(nameof(NewCurriculumName), "Tên khung chương trình không được để trống.");
                Curriculums = await _curriculumService.GetAllCurriculumsAsync();
                return Page();
            }

            if (NewCurriculumName.Trim().Length >= 50)
            {
                ModelState.AddModelError(nameof(NewCurriculumName), "Tên khung chương trình phải dưới 50 ký tự.");
                Curriculums = await _curriculumService.GetAllCurriculumsAsync();
                return Page();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            var curriculum = new Flipped_Classroom.Models.Curriculum
            {
                CurriculumName = NewCurriculumName.Trim(),
                Description = NewDescription?.Trim(),
                ManagerId = userId
            };

            await _curriculumService.CreateCurriculumAsync(curriculum);

            TempData["SuccessMessage"] = "Tạo khung chương trình mới thành công!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var (success, error) = await _curriculumService.DeleteCurriculumAsync(id);
            if (success)
                TempData["SuccessMessage"] = "Đã xóa khung chương trình thành công!";
            else
                TempData["ErrorMessage"] = error ?? "Không thể xóa khung chương trình.";
            return RedirectToPage();
        }
    }
}
