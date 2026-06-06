using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;

namespace Flipped_Classroom.Services.Implementation
{
    public class AssignmentService : IAssignmentService
    {
        private readonly Swp391NihongoContext _context;

        public AssignmentService(Swp391NihongoContext context)
        {
            _context = context;
        }

        public async Task<List<Assignment>> GetAssignmentsByClassAsync(int classId)
        {
            // Lấy danh sách bài tập của lớp học kèm theo thông tin Node chương trình (nếu có)
            return await _context.Assignments
                .Include(a => a.Node)
                .Where(a => a.ClassId == classId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Assignment?> GetAssignmentByIdAsync(int assignmentId)
        {
            // Lấy thông tin bài tập theo ID
            return await _context.Assignments
                .Include(a => a.Node)
                .Include(a => a.Class)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
        }

        public async Task<Assignment> CreateAssignmentAsync(Assignment assignment)
        {
            // Thiết lập ngày tạo mặc định nếu chưa có
            if (assignment.CreatedAt == null)
            {
                assignment.CreatedAt = DateTime.Now;
            }

            // Thêm bài tập mới vào cơ sở dữ liệu
            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();
            return assignment;
        }

        public async Task<bool> DeleteAssignmentAsync(int assignmentId)
        {
            // Tìm bài tập cần xóa
            var assignment = await _context.Assignments.FindAsync(assignmentId);
            if (assignment == null)
            {
                return false;
            }

            // Xóa bài tập và lưu thay đổi
            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
