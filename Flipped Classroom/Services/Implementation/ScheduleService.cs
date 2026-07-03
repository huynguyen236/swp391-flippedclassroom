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
            return await _context.Classes
                .Include(c => c.ClassSchedules)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
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

            var newSchedules = ScheduleSlotHelper.GenerateSchedules(
                classId,
                targetClass.StartDate.Value,
                targetClass.EndDate.Value,
                slotName
            );

            if (!newSchedules.Any())
                return (false, $"Không tạo được buổi học nào với slot {slotName}. Kiểm tra lại ngày bắt đầu/kết thúc.");

            // Kiểm tra trùng lịch giảng dạy của Giáo viên phụ trách lớp
            var teacherId = targetClass.ManagerId;
            var startDate = targetClass.StartDate.Value;
            var endDate = targetClass.EndDate.Value;

            // Lấy tất cả các buổi học hiện tại của giáo viên đó ở các lớp khác trong khoảng thời gian này
            var existingTeacherSchedules = await _context.ClassSchedules
                .Include(s => s.Class)
                .Where(s => s.Class.ManagerId == teacherId 
                         && s.ClassId != classId
                         && s.StudyDate >= startDate 
                         && s.StudyDate <= endDate)
                .ToListAsync();

            foreach (var ns in newSchedules)
            {
                var conflict = existingTeacherSchedules.FirstOrDefault(es => 
                    es.StudyDate == ns.StudyDate &&
                    es.StartTime < ns.EndTime &&
                    es.EndTime > ns.StartTime
                );

                if (conflict != null)
                {
                    string teacherName = targetClass.Manager != null 
                        ? $"{targetClass.Manager.FirstName} {targetClass.Manager.LastName}" 
                        : "Giáo viên quản lý";
                    return (false, $"Lịch giảng dạy bị trùng với Giáo viên '{teacherName}' " +
                                  $"tại lớp '{conflict.Class.ClassName}' vào ngày {ns.StudyDate:dd/MM/yyyy} " +
                                  $"khung giờ {conflict.StartTime:HH:mm} - {conflict.EndTime:HH:mm}.");
                }
            }

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
