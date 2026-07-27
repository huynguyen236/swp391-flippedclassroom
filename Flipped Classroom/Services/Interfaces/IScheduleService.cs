using System.Collections.Generic;
using System.Threading.Tasks;
using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface IScheduleService
    {
        // Phục vụ màn hình lịch học của Học sinh (Student)
        Task<List<ClassSchedule>> GetStudentSchedulesAsync(int studentUserId);
        Task<List<Class>> GetStudentEnrolledClassesAsync(int studentUserId);

        // Phục vụ màn hình quản lý danh sách lịch học của Manager
        Task<List<Class>> GetClassScheduleOverviewListAsync();
        Task<(bool Success, string Message)> AssignSlotToClassAsync(int classId, string slotName);

        /// <summary>
        /// Kiểm tra slot có trùng lịch giảng dạy của giáo viên hay không.
        /// Dùng được cho cả lớp chưa tồn tại (excludeClassId = null).
        /// </summary>
        Task<(bool Ok, string Message)> ValidateSlotForTeacherAsync(
            int teacherId,
            DateOnly startDate,
            DateOnly endDate,
            string slotName,
            int? excludeClassId = null
        );

        Task<bool> RemoveScheduleFromClassAsync(int classId);

        // Phục vụ màn hình chi tiết lịch và gán phòng học của Manager
        Task<Class?> GetClassWithSchedulesAsync(int classId);
        Task<bool> UpdateRoomForScheduleAsync(int scheduleId, string? roomName);
        Task<bool> UpdateAllRoomsForClassAsync(int classId, string? roomName);
    }
}
