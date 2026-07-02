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
        Task<bool> RemoveScheduleFromClassAsync(int classId);

        // Phục vụ màn hình chi tiết lịch và gán phòng học của Manager
        Task<Class?> GetClassWithSchedulesAsync(int classId);
        Task<bool> UpdateRoomForScheduleAsync(int scheduleId, string? roomName);
        Task<bool> UpdateAllRoomsForClassAsync(int classId, string? roomName);
    }
}
