using System.Collections.Generic;
using System.Threading.Tasks;
using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface IAssignmentService
    {
        Task<List<Assignment>> GetAssignmentsByClassAsync(int classId);
        Task<Assignment?> GetAssignmentByIdAsync(int assignmentId);
        Task<Assignment> CreateAssignmentAsync(Assignment assignment);
        Task<bool> DeleteAssignmentAsync(int assignmentId);
    }

    public record CreateAssignmentRequest(
        int ClassId,
        int? NodeId,
        string Title,
        string? RequirementText,
        string Type, // e.g. "File", "Text"
        System.DateTime DueDate
    );

    public record StudentAssignmentDto(
        int AssignmentId,
        string Title,
        string? RequirementText,
        System.DateTime? DueDate,
        string Status, // "Pending", "Submitted", "Graded"
        string? MediaUrl,
        decimal? Score,
        string? Feedback
    );
}
