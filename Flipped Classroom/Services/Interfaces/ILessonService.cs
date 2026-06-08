using System.Collections.Generic;
using System.Threading.Tasks;
using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface ILessonService
    {
        Task<Class?> GetClassWithMembersAsync(int classId);
        Task<Node?> GetNodeWithMaterialsAsync(int nodeId);

        // Khóa / mở node theo từng class
        Task<bool> IsNodeUnlockedAsync(int classId, int nodeId);
        Task<Dictionary<int, bool>> GetNodeUnlockStatusAsync(int classId);
        Task ToggleNodeLockAsync(int classId, int nodeId);

        // Tiến độ hoàn thành node của học sinh trong từng class
        Task<Dictionary<int, bool>> GetNodeCompletionAsync(int classId, int studentId);
        Task<bool> IsNodeCompletedAsync(int classId, int nodeId, int studentId);
        Task SetNodeCompletionAsync(int classId, int nodeId, int studentId, bool isCompleted);
    }
}
