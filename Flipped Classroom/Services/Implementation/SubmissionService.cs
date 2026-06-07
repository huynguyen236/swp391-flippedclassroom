using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;

namespace Flipped_Classroom.Services.Implementation
{
    public class SubmissionService : ISubmissionService
    {
        private readonly Swp391NihongoContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const long MaxFileSize = 15 * 1024 * 1024; // 15 MB

        public SubmissionService(Swp391NihongoContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<Submission?> GetSubmissionAsync(int assignmentId, int studentId)
        {
            // Tìm bài nộp của một học viên cụ thể đối với một bài tập
            return await _context.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
        }

        public async Task<List<Submission>> GetSubmissionsByAssignmentAsync(int assignmentId)
        {
            // Lấy danh sách tất cả bài nộp của một bài tập kèm theo thông tin của học viên
            return await _context.Submissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == assignmentId)
                .OrderByDescending(s => s.SubmitAt)
                .ToListAsync();
        }

        public async Task<Submission> SubmitAssignmentAsync(int assignmentId, int studentId, IFormFile file, string? contentText)
        {
            // 1. Kiểm tra sự tồn tại của bài tập và lớp học tương ứng
            var assignment = await _context.Assignments
                .Include(a => a.Class)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bài tập.");
            }

            // 2. Kiểm tra xem học viên có thực sự thuộc lớp học này không
            var isMember = await _context.ClassMembers
                .AnyAsync(cm => cm.ClassId == assignment.ClassId && cm.UserId == studentId);

            if (!isMember)
            {
                throw new UnauthorizedAccessException("Bạn không phải học viên của lớp học này.");
            }

            // 3. Kiểm tra xem đã quá hạn nộp bài chưa
            if (assignment.DueDate.HasValue && DateTime.Now > assignment.DueDate.Value)
            {
                throw new InvalidOperationException("Hạn nộp bài tập đã qua. Không thể nộp bài mới.");
            }

            // 4. Kiểm tra kích thước tệp tải lên (Tối đa 15MB)
            if (file.Length > MaxFileSize)
            {
                throw new ArgumentException("File size exceeds maximum limit of 15MB.");
            }

            // 5. Kiểm tra phần mở rộng tệp và MIME-type (Chỉ cho phép .docx và .mp3)
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".docx" && extension != ".mp3")
            {
                throw new ArgumentException("Only .docx and .mp3 files are permitted.");
            }

            var mimeType = file.ContentType.ToLowerInvariant();
            if (extension == ".docx" && mimeType != "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                throw new ArgumentException("Only .docx and .mp3 files are permitted.");
            }
            if (extension == ".mp3" && mimeType != "audio/mpeg" && mimeType != "audio/mp3")
            {
                throw new ArgumentException("Only .docx and .mp3 files are permitted.");
            }

            // 6. Tạo thư mục lưu trữ nếu chưa có
            var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "submissions");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // 7. Kiểm tra bài nộp cũ của học viên đối với bài tập này để ghi đè (xóa tệp cũ)
            var existingSubmission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);

            if (existingSubmission != null && !string.IsNullOrEmpty(existingSubmission.MediaUrl))
            {
                var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingSubmission.MediaUrl.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            // 8. Lưu tệp mới với tên được sinh ngẫu nhiên an toàn
            var uniqueFileName = $"{assignmentId}_{studentId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var mediaUrl = $"/uploads/submissions/{uniqueFileName}";

            // 9. Cập nhật cơ sở dữ liệu
            if (existingSubmission != null)
            {
                existingSubmission.MediaUrl = mediaUrl;
                existingSubmission.ContentText = contentText;
                existingSubmission.SubmitAt = DateTime.Now;
                existingSubmission.Status = "Submitted";
                existingSubmission.Score = null;     // Đặt lại điểm số khi nộp lại
                existingSubmission.Feedback = null;  // Đặt lại nhận xét khi nộp lại

                _context.Submissions.Update(existingSubmission);
                await _context.SaveChangesAsync();
                return existingSubmission;
            }
            else
            {
                var newSubmission = new Submission
                {
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    MediaUrl = mediaUrl,
                    ContentText = contentText,
                    SubmitAt = DateTime.Now,
                    Status = "Submitted"
                };

                _context.Submissions.Add(newSubmission);
                await _context.SaveChangesAsync();
                return newSubmission;
            }
        }

        public async Task<bool> GradeSubmissionAsync(int submissionId, decimal score, string? feedback)
        {
            // 1. Kiểm tra thang điểm hợp lệ (0.0 đến 10.0)
            if (score < 0m || score > 10m)
            {
                throw new ArgumentException("Score must be between 0.0 and 10.0.");
            }

            // 2. Tìm bài nộp cần chấm điểm
            var submission = await _context.Submissions.FindAsync(submissionId);
            if (submission == null)
            {
                return false;
            }

            // 3. Cập nhật thông tin điểm số và nhận xét
            submission.Score = score;
            submission.Feedback = feedback;
            submission.Status = "Graded";

            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
