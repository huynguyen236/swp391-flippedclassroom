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

namespace Flipped_Classroom.Pages.StudentClasses
{
    [Authorize(Roles = "Student")]
    public class ScheduleModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public ScheduleModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lịch học gộp từ tất cả lớp student đã tham gia, mỗi item kèm thông tin lớp.
        /// </summary>
        public List<StudentScheduleItem> ScheduleItems { get; set; } = new();

        /// <summary>
        /// Danh sách lớp student tham gia (để hiện filter / legend).
        /// </summary>
        public List<ClassInfo> EnrolledClasses { get; set; } = new();

        // Calendar display
        public int DisplayMonth { get; set; }
        public int DisplayYear { get; set; }

        /// <summary>
        /// Set ngày có lịch trong tháng đang hiển thị.
        /// </summary>
        public HashSet<DateOnly> ScheduleDates { get; set; } = new();

        // View-model classes
        public class StudentScheduleItem
        {
            public int ScheduleId { get; set; }
            public int ClassId { get; set; }
            public string ClassName { get; set; } = string.Empty;
            public DateOnly StudyDate { get; set; }
            public TimeOnly StartTime { get; set; }
            public TimeOnly EndTime { get; set; }
            public string? Room { get; set; }
            public string? DetectedSlot { get; set; }
        }

        public class ClassInfo
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; } = string.Empty;
            public string? DetectedSlot { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? month, int? year)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            // Lấy tất cả lớp student đã tham gia + lịch học
            var enrolledClasses = await _context.Classes
                .Include(c => c.ClassSchedules)
                .Where(c => c.ClassMembers.Any(cm => cm.UserId == userId))
                .ToListAsync();

            var classInfoList = new List<ClassInfo>();
            var allItems = new List<StudentScheduleItem>();

            foreach (var cls in enrolledClasses)
            {
                // Detect slot cho lớp này
                string? detectedSlot = ScheduleSlotHelper.DetectSlot(cls.ClassSchedules);

                classInfoList.Add(new ClassInfo
                {
                    ClassId = cls.Id,
                    ClassName = cls.ClassName,
                    DetectedSlot = detectedSlot
                });

                foreach (var schedule in cls.ClassSchedules)
                {
                    allItems.Add(new StudentScheduleItem
                    {
                        ScheduleId = schedule.Id,
                        ClassId = cls.Id,
                        ClassName = cls.ClassName,
                        StudyDate = schedule.StudyDate,
                        StartTime = schedule.StartTime,
                        EndTime = schedule.EndTime,
                        Room = schedule.Room,
                        DetectedSlot = detectedSlot
                    });
                }
            }

            ScheduleItems = allItems.OrderBy(s => s.StudyDate).ThenBy(s => s.StartTime).ToList();
            EnrolledClasses = classInfoList;
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
