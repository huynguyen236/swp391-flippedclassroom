using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Flipped_Classroom.Pages.Classrooms
{
    [Authorize(Roles = "Admin,Manager")]
    public class CreateModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;
        private readonly IQuizService _quizService;
        private readonly IScheduleService _scheduleService;

        public CreateModel(
            Flipped_Classroom.Data.Swp391NihongoContext context,
            IQuizService quizService,
            IScheduleService scheduleService
        )
        {
            _context = context;
            _quizService = quizService;
            _scheduleService = scheduleService;
        }

        public IActionResult OnGet()
        {
            LoadSelectLists();
            return Page();
        }

        [BindProperty]
        public Class Class { get; set; } = default!;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng chọn slot học cho lớp.")]
        public string SlotName { get; set; } = string.Empty;

        public List<ScheduleSlotHelper.SlotDefinition> AvailableSlots { get; set; } = new();

        /// <summary>
        /// Kiểm tra trùng lịch ngay trên form (AJAX), trước khi Manager bấm Tạo.
        /// Đây chỉ là lớp UX — validate thật vẫn nằm ở OnPostAsync.
        /// </summary>
        public async Task<IActionResult> OnGetCheckSlotAsync(
            int managerId,
            string? startDate,
            string? endDate,
            string? slotName
        )
        {
            if (managerId <= 0 || string.IsNullOrWhiteSpace(slotName))
            {
                return new JsonResult(new { ok = true, message = "" });
            }

            if (
                !DateOnly.TryParse(startDate, out var start)
                || !DateOnly.TryParse(endDate, out var end)
            )
            {
                return new JsonResult(new { ok = true, message = "" });
            }

            var result = await _scheduleService.ValidateSlotForTeacherAsync(
                managerId,
                start,
                end,
                slotName
            );

            return new JsonResult(new { ok = result.Ok, message = result.Message });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove navigation properties from validation
            ModelState.Remove("Class.Manager");
            ModelState.Remove("Class.Curriculum");

            // Bắt buộc chọn khung chương trình khi tạo lớp
            if (Class == null || Class.CurriculumId <= 0)
            {
                ModelState.AddModelError(
                    "Class.CurriculumId",
                    "Vui lòng chọn khung chương trình cho lớp."
                );
            }

            if (Class == null || Class.ManagerId <= 0)
            {
                ModelState.AddModelError(
                    "Class.ManagerId",
                    "Vui lòng chọn giáo viên phụ trách lớp."
                );
            }

            // Slot cần ngày bắt đầu/kết thúc mới sinh được buổi học ⇒ giờ là bắt buộc
            if (Class?.StartDate == null)
            {
                ModelState.AddModelError("Class.StartDate", "Vui lòng chọn ngày bắt đầu.");
            }

            if (Class?.EndDate == null)
            {
                ModelState.AddModelError("Class.EndDate", "Vui lòng chọn ngày kết thúc.");
            }

            if (!ModelState.IsValid || _context.Classes == null || Class == null)
            {
                return await BackToFormAsync();
            }

            // Validate trùng lịch giảng viên TRƯỚC khi ghi bất cứ thứ gì vào DB
            var check = await _scheduleService.ValidateSlotForTeacherAsync(
                Class.ManagerId,
                Class.StartDate!.Value,
                Class.EndDate!.Value,
                SlotName
            );

            if (!check.Ok)
            {
                ModelState.AddModelError(string.Empty, check.Message);
                return await BackToFormAsync();
            }

            // Tạo lớp + lịch học + clone quiz trong cùng một transaction
            List<ClassSchedule> schedules;
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                Class.CreatedAt = DateTime.Now;
                Class.Status = "Active";
                // Generate a 6-character uppercase alphanumeric invite code
                Class.InviteCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

                _context.Classes.Add(Class);
                await _context.SaveChangesAsync(); // cần Class.Id để gắn lịch

                schedules = ScheduleSlotHelper.GenerateSchedules(
                    Class.Id,
                    Class.StartDate.Value,
                    Class.EndDate.Value,
                    SlotName
                );
                _context.ClassSchedules.AddRange(schedules);
                await _context.SaveChangesAsync();

                // Clone template quizzes from Curriculum to the newly created Class
                await _quizService.CloneCurriculumQuizzesToClassAsync(Class.CurriculumId, Class.Id);

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(
                    string.Empty,
                    "Có lỗi khi tạo lớp học. Vui lòng thử lại."
                );
                return await BackToFormAsync();
            }

            TempData["Success"] =
                $"Đã tạo lớp '{Class.ClassName}' với slot {SlotName} — {schedules.Count} buổi học.";

            return RedirectToPage("./Index");
        }

        private async Task<IActionResult> BackToFormAsync()
        {
            if (Class != null && Class.ManagerId > 0)
            {
                Class.Manager = await _context.Users.FindAsync(Class.ManagerId);
            }
            LoadSelectLists();
            return Page();
        }

        private void LoadSelectLists()
        {
            var managers = _context
                .Users.Where(u => u.Role == "Teacher")
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToList();
            ViewData["ManagerId"] = new SelectList(managers, "Id", "FullName");

            var curriculums = _context.Curriculums.OrderBy(c => c.CurriculumName).ToList();
            ViewData["CurriculumId"] = new SelectList(curriculums, "Id", "CurriculumName");

            AvailableSlots = ScheduleSlotHelper.GetAllSlots();
        }
    }
}
