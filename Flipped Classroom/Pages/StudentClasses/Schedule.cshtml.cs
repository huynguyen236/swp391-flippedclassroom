using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.StudentClasses
{
    [Authorize(Roles = "Student")]
    public class ScheduleModel : PageModel
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleModel(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        /// <summary>
        /// Lịch học gộp từ tất cả lớp student đã tham gia, mỗi item kèm thông tin lớp.
        /// </summary>
        public List<ClassSchedule> ScheduleItems { get; set; } = new();

        /// <summary>
        /// Danh sách lớp student tham gia (để hiện filter / legend).
        /// </summary>
        public List<Class> EnrolledClasses { get; set; } = new();

        // Calendar display
        public int DisplayMonth { get; set; }
        public int DisplayYear { get; set; }

        /// <summary>
        /// Set ngày có lịch trong tháng đang hiển thị.
        /// </summary>
        public HashSet<DateOnly> ScheduleDates { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? month, int? year)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            ScheduleItems = await _scheduleService.GetStudentSchedulesAsync(userId);
            EnrolledClasses = await _scheduleService.GetStudentEnrolledClassesAsync(userId);
            ScheduleDates = new HashSet<DateOnly>(ScheduleItems.Select(s => s.StudyDate));

            // Xác định tháng hiển thị
            if (month.HasValue && year.HasValue)
            {
                DisplayMonth = month.Value;
                DisplayYear = year.Value;
            }
            else if (ScheduleItems.Any())
            {
                var now = DateOnly.FromDateTime(DateTime.Now);
                if (ScheduleDates.Any(d => d.Month == now.Month && d.Year == now.Year))
                {
                    DisplayMonth = now.Month;
                    DisplayYear = now.Year;
                }
                else
                {
                    var firstSchedule = ScheduleItems.First();
                    DisplayMonth = firstSchedule.StudyDate.Month;
                    DisplayYear = firstSchedule.StudyDate.Year;
                }
            }
            else
            {
                DisplayMonth = DateTime.Now.Month;
                DisplayYear = DateTime.Now.Year;
            }

            return Page();
        }
    }
}
