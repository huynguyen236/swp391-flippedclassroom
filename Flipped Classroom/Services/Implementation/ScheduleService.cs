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
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (targetClass == null)
                return (false, "Lớp học không tồn tại.");

            if (targetClass.StartDate == null || targetClass.EndDate == null)
                return (false, "Lớp chưa có ngày bắt đầu hoặc kết thúc. Vui lòng cập nhật trước khi gán lịch.");

            if (targetClass.ClassSchedules.Any())
            {
                _context.ClassSchedules.RemoveRange(targetClass.ClassSchedules);
            }

            var newSchedules = ScheduleSlotHelper.GenerateSchedules(
                classId,
                targetClass.StartDate.Value,
                targetClass.EndDate.Value,
                slotName
            );

            if (!newSchedules.Any())
                return (false, $"Không tạo được buổi học nào với slot {slotName}. Kiểm tra lại ngày bắt đầu/kết thúc.");

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
