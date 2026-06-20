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
    public class DetailsModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public DetailsModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public Class TargetClass { get; set; } = null!;
        public List<ClassSchedule> Schedules { get; set; } = new();
        public string? DetectedSlot { get; set; }

        // For calendar view
        public int DisplayMonth { get; set; }
        public int DisplayYear { get; set; }
        public HashSet<DateOnly> ScheduleDates { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int classId, int? month, int? year)
        {
            var targetClass = await _context.Classes
                .Include(c => c.ClassSchedules)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (targetClass == null)
                return NotFound();

            TargetClass = targetClass;
            Schedules = targetClass.ClassSchedules.OrderBy(s => s.StudyDate).ToList();
            ScheduleDates = new HashSet<DateOnly>(Schedules.Select(s => s.StudyDate));

            // Detect slot
            DetectedSlot = ScheduleSlotHelper.DetectSlot(Schedules);

            // Calendar display month
            if (month.HasValue && year.HasValue)
            {
                DisplayMonth = month.Value;
                DisplayYear = year.Value;
            }
            else if (Schedules.Any())
            {
                // Default to the first schedule month
                DisplayMonth = Schedules.First().StudyDate.Month;
                DisplayYear = Schedules.First().StudyDate.Year;
            }
            else
            {
                DisplayMonth = DateTime.Now.Month;
                DisplayYear = DateTime.Now.Year;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateRoomAsync(int scheduleId, string? roomName, int classId)
        {
            var schedule = await _context.ClassSchedules.FindAsync(scheduleId);
            if (schedule == null)
                return NotFound();

            schedule.Room = roomName;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật phòng học thành '{roomName ?? "Trực tuyến"}' cho buổi học ngày {schedule.StudyDate.ToString("dd/MM/yyyy")}.";
            return RedirectToPage(new { classId });
        }

        public async Task<IActionResult> OnPostUpdateAllRoomsAsync(int classId, string? roomName)
        {
            var schedules = await _context.ClassSchedules
                .Where(s => s.ClassId == classId)
                .ToListAsync();

            foreach (var s in schedules)
            {
                s.Room = roomName;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật phòng học thành '{roomName ?? "Trực tuyến"}' cho toàn bộ {schedules.Count} buổi học.";
            return RedirectToPage(new { classId });
        }
    }
}
