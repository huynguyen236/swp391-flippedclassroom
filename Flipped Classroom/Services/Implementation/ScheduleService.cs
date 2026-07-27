using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Services.Implementation
{
    public class ScheduleService : IScheduleService
    {
        private readonly Swp391NihongoContext _context;

        public ScheduleService(Swp391NihongoContext context)
        {
            _context = context;
        }

        public async Task<List<ClassSchedule>> GetStudentSchedulesAsync(int studentUserId)
        {
            return await _context.ClassSchedules
                .Include(s => s.Class)
                .ThenInclude(c => c.ClassSchedules)
                .Where(s => s.Class.ClassMembers.Any(cm => cm.UserId == studentUserId))
                .OrderBy(s => s.StudyDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<Class>> GetStudentEnrolledClassesAsync(int studentUserId)
        {
            return await _context.Classes
                .Include(c => c.ClassSchedules)
                .Where(c => c.ClassMembers.Any(cm => cm.UserId == studentUserId))
                .ToListAsync();
        }

        public async Task<List<Class>> GetClassScheduleOverviewListAsync()
        {
            await _context.AutoInactivateExpiredClassesAsync();

            return await _context.Classes
                .Include(c => c.ClassSchedules)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<(bool Ok, string Message)> ValidateSlotForTeacherAsync(
            int teacherId,
            DateOnly startDate,
            DateOnly endDate,
            string slotName,
            int? excludeClassId = null
        )
        {
            if (string.IsNullOrWhiteSpace(slotName))
                return (false, "Vui lòng chọn slot học cho lớp.");

            if (endDate < startDate)
                return (false, "Ngày kết thúc phải sau ngày bắt đầu.");

            // classId = 0 vì đây chỉ là danh sách tạm để tính ngày/giờ, không ghi vào DB
            var newSchedules = ScheduleSlotHelper.GenerateSchedules(0, startDate, endDate, slotName);

            if (!newSchedules.Any())
                return (
                    false,
                    $"Không tạo được buổi học nào với slot {slotName}. Kiểm tra lại ngày bắt đầu/kết thúc."
                );

            // Lấy tất cả các buổi học hiện tại của giáo viên đó ở các lớp khác trong khoảng thời gian này
            var existingTeacherSchedules = await _context
                .ClassSchedules.Include(s => s.Class)
                .Where(s =>
                    s.Class.ManagerId == teacherId
                    && (excludeClassId == null || s.ClassId != excludeClassId)
                    && s.StudyDate >= startDate
                    && s.StudyDate <= endDate
                )
                .ToListAsync();

            foreach (var ns in newSchedules)
            {
                var conflict = existingTeacherSchedules.FirstOrDefault(es =>
                    es.StudyDate == ns.StudyDate
                    && es.StartTime < ns.EndTime
                    && es.EndTime > ns.StartTime
                );

                if (conflict != null)
                {
                    var teacher = await _context.Users.FindAsync(teacherId);
                    string teacherName =
                        teacher != null
                            ? $"{teacher.FirstName} {teacher.LastName}"
                            : "Giáo viên được chọn";

                    return (
                        false,
                        $"Giáo viên '{teacherName}' đã có lịch dạy lớp '{conflict.Class.ClassName}' "
                            + $"vào ngày {ns.StudyDate:dd/MM/yyyy} khung giờ "
                            + $"{conflict.StartTime:HH\\:mm} - {conflict.EndTime:HH\\:mm}. "
                            + $"Vui lòng chọn slot khác hoặc giáo viên khác."
                    );
                }
            }

            return (true, $"Slot {slotName} khả dụng — sẽ tạo {newSchedules.Count} buổi học.");
        }

        public async Task<(bool Success, string Message)> AssignSlotToClassAsync(int classId, string slotName)
        {
            var targetClass = await _context.Classes
                .Include(c => c.ClassSchedules)
                .Include(c => c.Manager)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (targetClass == null)
                return (false, "Lớp học không tồn tại.");

            if (targetClass.StartDate == null || targetClass.EndDate == null)
                return (false, "Lớp chưa có ngày bắt đầu hoặc kết thúc. Vui lòng cập nhật trước khi gán lịch.");

            // Dùng chung một rule validate với màn hình Tạo lớp
            var check = await ValidateSlotForTeacherAsync(
                targetClass.ManagerId,
                targetClass.StartDate.Value,
                targetClass.EndDate.Value,
                slotName,
                excludeClassId: classId
            );

            if (!check.Ok)
                return (false, check.Message);

            var newSchedules = ScheduleSlotHelper.GenerateSchedules(
                classId,
                targetClass.StartDate.Value,
                targetClass.EndDate.Value,
                slotName
            );

            if (targetClass.ClassSchedules.Any())
            {
                _context.ClassSchedules.RemoveRange(targetClass.ClassSchedules);
            }

            _context.ClassSchedules.AddRange(newSchedules);
            await _context.SaveChangesAsync();

            return (true, $"Đã gán slot {slotName} cho lớp {targetClass.ClassName} — tạo {newSchedules.Count} buổi học.");
        }

        public async Task<bool> RemoveScheduleFromClassAsync(int classId)
        {
            var targetClass = await _context.Classes
                .Include(c => c.ClassSchedules)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (targetClass == null)
                return false;

            if (targetClass.ClassSchedules.Any())
            {
                _context.ClassSchedules.RemoveRange(targetClass.ClassSchedules);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<Class?> GetClassWithSchedulesAsync(int classId)
        {
            return await _context.Classes
                .Include(c => c.ClassSchedules)
                .FirstOrDefaultAsync(c => c.Id == classId);
        }

        public async Task<bool> UpdateRoomForScheduleAsync(int scheduleId, string? roomName)
        {
            var schedule = await _context.ClassSchedules.FindAsync(scheduleId);
            if (schedule == null)
                return false;

            schedule.Room = roomName;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAllRoomsForClassAsync(int classId, string? roomName)
        {
            var schedules = await _context.ClassSchedules
                .Where(s => s.ClassId == classId)
                .ToListAsync();

            foreach (var s in schedules)
            {
                s.Room = roomName;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
