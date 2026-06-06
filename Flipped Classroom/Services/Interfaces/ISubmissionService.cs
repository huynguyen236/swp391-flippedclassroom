using System.Collections.Generic;
using System.Threading.Tasks;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Http;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface ISubmissionService
    {
        Task<Submission?> GetSubmissionAsync(int assignmentId, int studentId);
        Task<List<Submission>> GetSubmissionsByAssignmentAsync(int assignmentId);
        Task<Submission> SubmitAssignmentAsync(
            int assignmentId,
            int studentId,
            IFormFile file,
            string? contentText
        );
        Task<bool> GradeSubmissionAsync(int submissionId, decimal score, string? feedback);
    }

    public class SubmitAssignmentForm
    {
        public int AssignmentId { get; set; }
        public IFormFile UploadedFile { get; set; } = null!;
        public string? ContentText { get; set; }
    }

    public record GradeSubmissionRequest(
        int SubmissionId,
        decimal Score, // Valid range: 0.0 to 10.0
        string? Feedback
    );
}
