using System;
using System.Collections.Generic;
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
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.Classrooms
{
    [Authorize(Roles = "Admin,Manager")]
    public class EditModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;
        private readonly IScheduleService _scheduleService;

        public EditModel(
            Flipped_Classroom.Data.Swp391NihongoContext context,
            IScheduleService scheduleService
        )
        {
            _context = context;
            _scheduleService = scheduleService;
        }

        [BindProperty]
        public Class Class { get; set; } = default!;

        /// <summary>
        /// Slot học của lớp. Để trống nghĩa là giữ nguyên lịch hiện tại (lớp chưa có lịch).
        /// </summary>
        [BindProperty]
        public string? SlotName { get; set; }

        public List<ScheduleSlotHelper.SlotDefinition> AvailableSlots { get; set; } = new();

        /// <summary>Số buổi học hiện có, để hiển thị cảnh báo sinh lại lịch.</summary>
        public int ExistingScheduleCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Classes == null)
            {
                return NotFound();
            }

            var classroom = await _context
                .Classes.Include(c => c.Manager)
                .Include(c => c.ClassSchedules)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classroom == null)
            {
                return NotFound();
            }
            Class = classroom;

            // Suy ra slot đang dùng từ lịch hiện tại để pre-select dropdown
            ExistingScheduleCount = classroom.ClassSchedules.Count;
            var detected = ScheduleSlotHelper.DetectSlot(classroom.ClassSchedules);
            SlotName = detected == "Custom" ? null : detected;

            LoadSelectLists();
            return Page();
        }

        private void LoadSelectLists()
        {
            LoadManagerSelectList();
            AvailableSlots = ScheduleSlotHelper.GetAllSlots();
        }

        private void LoadManagerSelectList()
        {
            var managers = _context
                .Users.Where(u => u.Role == "Teacher")
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToList();
            ViewData["ManagerId"] = new SelectList(managers, "Id", "FullName");
        }

        /// <summary>
        /// Kiểm tra trùng lịch ngay trên form (AJAX). Loại trừ chính lớp đang sửa.
        /// Đây chỉ là lớp UX — validate thật vẫn nằm ở OnPostAsync.
        /// </summary>
        public async Task<IActionResult> OnGetCheckSlotAsync(
            int classId,
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
                slotName,
                excludeClassId: classId
            );

            return new JsonResult(new { ok = result.Ok, message = result.Message });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Class.Manager");
            ModelState.Remove("Class.Curriculum");

            if (!ModelState.IsValid || Class == null)
            {
                return await BackToFormAsync();
            }

            var originalClass = await _context
                .Classes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == Class.Id);

            if (originalClass == null)
            {
                return NotFound();
            }

            var existingSchedules = await _context
                .ClassSchedules.Where(s => s.ClassId == Class.Id)
                .ToListAsync();

            // Slot bỏ trống ⇒ giữ nguyên lịch hiện tại; nếu lớp đã có lịch thì suy ra slot đang dùng
            var effectiveSlot = SlotName;
            if (string.IsNullOrWhiteSpace(effectiveSlot))
            {
                var detected = ScheduleSlotHelper.DetectSlot(existingSchedules);
                effectiveSlot = detected == "Custom" ? null : detected;
            }

            // Lịch phải được sinh lại khi giáo viên, khoảng ngày, hoặc slot thay đổi
            var teacherChanged = originalClass.ManagerId != Class.ManagerId;
            var datesChanged =
                originalClass.StartDate != Class.StartDate
                || originalClass.EndDate != Class.EndDate;
            var slotChanged =
                !string.IsNullOrWhiteSpace(SlotName)
                && SlotName != ScheduleSlotHelper.DetectSlot(existingSchedules);

            var mustRebuildSchedule =
                !string.IsNullOrWhiteSpace(effectiveSlot)
                && (teacherChanged || datesChanged || slotChanged);

            // Đổi giáo viên nhưng giữ nguyên lịch custom ⇒ vẫn phải check trùng trên lịch cũ
            var mustRevalidateOnly =
                teacherChanged && !mustRebuildSchedule && existingSchedules.Any();

            List<ClassSchedule>? newSchedules = null;

            if (mustRebuildSchedule)
            {
                if (Class.StartDate == null || Class.EndDate == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Lớp có lịch học nên cần cả ngày bắt đầu và ngày kết thúc."
                    );
                    return await BackToFormAsync();
                }

                var check = await _scheduleService.ValidateSlotForTeacherAsync(
                    Class.ManagerId,
                    Class.StartDate.Value,
                    Class.EndDate.Value,
                    effectiveSlot!,
                    excludeClassId: Class.Id
                );

                if (!check.Ok)
                {
                    ModelState.AddModelError(string.Empty, check.Message);
                    return await BackToFormAsync();
                }

                newSchedules = ScheduleSlotHelper.GenerateSchedules(
                    Class.Id,
                    Class.StartDate.Value,
                    Class.EndDate.Value,
                    effectiveSlot!
                );
            }
            else if (mustRevalidateOnly)
            {
                var conflictMessage = await FindConflictForTeacherAsync(
                    Class.ManagerId,
                    Class.Id,
                    existingSchedules
                );

                if (conflictMessage != null)
                {
                    ModelState.AddModelError(string.Empty, conflictMessage);
                    return await BackToFormAsync();
                }
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Attach(Class).State = EntityState.Modified;

                _context.Entry(Class).Property(x => x.CreatedAt).IsModified = false;
                _context.Entry(Class).Property(x => x.InviteCode).IsModified = false;
                _context.Entry(Class).Property(x => x.CurriculumId).IsModified = false;

                if (newSchedules != null)
                {
                    _context.ClassSchedules.RemoveRange(existingSchedules);
                    _context.ClassSchedules.AddRange(newSchedules);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                if (!ClassExists(Class.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(
                    string.Empty,
                    "Có lỗi khi lưu thay đổi. Vui lòng thử lại."
                );
                return await BackToFormAsync();
            }

            if (newSchedules != null)
            {
                TempData["Success"] =
                    $"Đã cập nhật lớp '{Class.ClassName}' và sinh lại lịch học — {newSchedules.Count} buổi.";
            }

            return RedirectToPage("./Index");
        }

        /// <summary>
        /// Kiểm tra bộ lịch đang có của lớp xem có trùng với lịch dạy khác của giáo viên hay không.
        /// Dùng cho lịch "Custom" — không khớp slot nào nên không sinh lại được.
        /// </summary>
        private async Task<string?> FindConflictForTeacherAsync(
            int teacherId,
            int classId,
            List<ClassSchedule> schedules
        )
        {
            var otherSchedules = await _context
                .ClassSchedules.Include(s => s.Class)
                .Where(s => s.Class.ManagerId == teacherId && s.ClassId != classId)
                .ToListAsync();

            foreach (var session in schedules)
            {
                var conflict = otherSchedules.FirstOrDefault(es =>
                    es.StudyDate == session.StudyDate
                    && es.StartTime < session.EndTime
                    && es.EndTime > session.StartTime
                );

                if (conflict != null)
                {
                    var teacher = await _context.Users.FindAsync(teacherId);
                    string teacherName =
                        teacher != null
                            ? $"{teacher.FirstName} {teacher.LastName}"
                            : "Giáo viên mới";

                    return $"Không thể đổi giáo viên. Giáo viên '{teacherName}' bị trùng lịch dạy ở lớp '{conflict.Class.ClassName}' "
                        + $"vào ngày {session.StudyDate:dd/MM/yyyy} lúc {conflict.StartTime:HH\\:mm} - {conflict.EndTime:HH\\:mm}.";
                }
            }

            return null;
        }

        private async Task<IActionResult> BackToFormAsync()
        {
            if (Class != null && Class.ManagerId > 0)
            {
                Class.Manager = await _context.Users.FindAsync(Class.ManagerId);
            }

            if (Class != null)
            {
                ExistingScheduleCount = await _context
                    .ClassSchedules.CountAsync(s => s.ClassId == Class.Id);
            }

            LoadSelectLists();
            return Page();
        }

        private bool ClassExists(int id)
        {
            return (_context.Classes?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
