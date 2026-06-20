using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.Schedules
{
    [Authorize(Roles = "Admin,Manager")]
    public class IndexModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public IndexModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public IList<ClassScheduleViewModel> ClassList { get; set; } = new List<ClassScheduleViewModel>();
        public List<ScheduleSlotHelper.SlotDefinition> AvailableSlots { get; set; } = new();

        public class ClassScheduleViewModel
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; } = string.Empty;
            public DateOnly? StartDate { get; set; }
            public DateOnly? EndDate { get; set; }
            public string? Status { get; set; }
            public int ScheduleCount { get; set; }
            public string? AssignedSlot { get; set; }
        }

        public async Task OnGetAsync()
        {
            var classes = await _context.Classes
                .Include(c => c.ClassSchedules)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ClassList = classes.Select(c =>
            {
                // Detect assigned slot by matching schedule times
                string? assignedSlot = ScheduleSlotHelper.DetectSlot(c.ClassSchedules);

                return new ClassScheduleViewModel
                {
                    ClassId = c.Id,
                    ClassName = c.ClassName,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Status = c.Status,
                    ScheduleCount = c.ClassSchedules.Count,
                    AssignedSlot = assignedSlot
                };
            }).ToList();

            AvailableSlots = ScheduleSlotHelper.GetAllSlots();
        }

        public async Task<IActionResult> OnPostAssignSlotAsync(int classId, string slotName)
        {
            var targetClass = await _context.Classes
                .Include(c => c.ClassSchedules)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (targetClass == null)
                return NotFound();

            if (targetClass.StartDate == null || targetClass.EndDate == null)
            {
                TempData["Error"] = "Lớp chưa có ngày bắt đầu hoặc kết thúc. Vui lòng cập nhật trước khi gán lịch.";
                return RedirectToPage();
            }

            // Remove old schedules
            if (targetClass.ClassSchedules.Any())
            {
                _context.ClassSchedules.RemoveRange(targetClass.ClassSchedules);
            }

            // Generate new schedules
            var newSchedules = ScheduleSlotHelper.GenerateSchedules(
                classId,
                targetClass.StartDate.Value,
                targetClass.EndDate.Value,
                slotName
            );

            if (!newSchedules.Any())
            {
                TempData["Error"] = $"Không tạo được buổi học nào với slot {slotName}. Kiểm tra lại ngày bắt đầu/kết thúc.";
                return RedirectToPage();
            }

            _context.ClassSchedules.AddRange(newSchedules);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã gán slot {slotName} cho lớp {targetClass.ClassName} — tạo {newSchedules.Count} buổi học.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveScheduleAsync(int classId)
        {
            var targetClass = await _context.Classes
                .Include(c => c.ClassSchedules)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (targetClass == null)
                return NotFound();

            if (targetClass.ClassSchedules.Any())
            {
                _context.ClassSchedules.RemoveRange(targetClass.ClassSchedules);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Đã xóa lịch học của lớp {targetClass.ClassName}.";
            return RedirectToPage();
        }
    }
}
