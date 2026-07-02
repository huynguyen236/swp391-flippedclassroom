using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.Schedules
{
    [Authorize(Roles = "Admin,Manager")]
    public class DetailsModel : PageModel
    {
        private readonly IScheduleService _scheduleService;

        public DetailsModel(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
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
            var targetClass = await _scheduleService.GetClassWithSchedulesAsync(classId);
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
            var success = await _scheduleService.UpdateRoomForScheduleAsync(scheduleId, roomName);
            if (!success)
                return NotFound();

            TempData["Success"] = $"Đã cập nhật phòng học thành '{roomName ?? "Trực tuyến"}' cho buổi học.";
            return RedirectToPage(new { classId });
        }

        public async Task<IActionResult> OnPostUpdateAllRoomsAsync(int classId, string? roomName)
        {
            await _scheduleService.UpdateAllRoomsForClassAsync(classId, roomName);
            TempData["Success"] = $"Đã cập nhật phòng học thành '{roomName ?? "Trực tuyến"}' cho toàn bộ buổi học.";
            return RedirectToPage(new { classId });
        }
    }
}
