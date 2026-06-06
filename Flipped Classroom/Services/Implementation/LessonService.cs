using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flipped_Classroom.Services.Implementation
{
    public class LessonService : ILessonService
    {
        private readonly Swp391NihongoContext _db;
        private readonly ILogger<LessonService> _logger;

        public LessonService(Swp391NihongoContext db, ILogger<LessonService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<Class?> GetClassWithMembersAsync(int classId)
        {
            try
            {
                return await _db.Classes
                    .Include(c => c.ClassMembers)
                    .FirstOrDefaultAsync(c => c.Id == classId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lớp học ID: {ClassId}", classId);
                return null;
            }
        }

        public async Task<Node?> GetNodeWithMaterialsAsync(int nodeId)
        {
            try
            {
                return await _db.Nodes
                    .Include(n => n.Materials)
                    .Include(n => n.ParentNode)
                    .FirstOrDefaultAsync(n => n.Id == nodeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy bài học (node) ID: {NodeId}", nodeId);
                return null;
            }
        }

        public async Task<bool> IsNodeUnlockedAsync(int classId, int nodeId)
        {
            try
            {
                var status = await _db.ClassNodeStatuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ClassId == classId && s.NodeId == nodeId);

                // Không có bản ghi = khóa
                return status?.IsUnlocked ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái mở node {NodeId} của lớp {ClassId}", nodeId, classId);
                return false;
            }
        }

        public async Task<Dictionary<int, bool>> GetNodeUnlockStatusAsync(int classId)
        {
            try
            {
                return await _db.ClassNodeStatuses
                    .AsNoTracking()
                    .Where(s => s.ClassId == classId)
                    .ToDictionaryAsync(s => s.NodeId, s => s.IsUnlocked);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy trạng thái mở node của lớp {ClassId}", classId);
                return new Dictionary<int, bool>();
            }
        }

        public async Task ToggleNodeLockAsync(int classId, int nodeId)
        {
            try
            {
                var status = await _db.ClassNodeStatuses
                    .FirstOrDefaultAsync(s => s.ClassId == classId && s.NodeId == nodeId);

                if (status == null)
                {
                    // Chưa có bản ghi (đang khóa) → tạo mới và mở
                    status = new ClassNodeStatus
                    {
                        ClassId = classId,
                        NodeId = nodeId,
                        IsUnlocked = true,
                        UnlockedAt = DateTime.Now
                    };
                    _db.ClassNodeStatuses.Add(status);
                }
                else
                {
                    status.IsUnlocked = !status.IsUnlocked;
                    status.UnlockedAt = status.IsUnlocked ? DateTime.Now : null;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("Lớp {ClassId} đã đổi trạng thái node {NodeId} thành {State}",
                    classId, nodeId, status.IsUnlocked ? "MỞ" : "KHÓA");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi trạng thái khóa node {NodeId} của lớp {ClassId}", nodeId, classId);
                throw;
            }
        }

        public async Task<Dictionary<int, bool>> GetNodeCompletionAsync(int classId, int studentId)
        {
            try
            {
                return await _db.StudentProgresses
                    .AsNoTracking()
                    .Where(p => p.ClassId == classId && p.StudentId == studentId)
                    .ToDictionaryAsync(p => p.NodeId, p => p.IsCompleted ?? false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tiến độ học của học sinh {StudentId} trong lớp {ClassId}", studentId, classId);
                return new Dictionary<int, bool>();
            }
        }

        public async Task<bool> IsNodeCompletedAsync(int classId, int nodeId, int studentId)
        {
            try
            {
                var progress = await _db.StudentProgresses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ClassId == classId && p.NodeId == nodeId && p.StudentId == studentId);
                return progress?.IsCompleted ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra hoàn thành node {NodeId} của học sinh {StudentId} trong lớp {ClassId}", nodeId, studentId, classId);
                return false;
            }
        }

        // Đặt trạng thái hoàn thành theo lựa chọn của học sinh (tích/bỏ tích)
        public async Task SetNodeCompletionAsync(int classId, int nodeId, int studentId, bool isCompleted)
        {
            try
            {
                var progress = await _db.StudentProgresses
                    .FirstOrDefaultAsync(p => p.ClassId == classId && p.NodeId == nodeId && p.StudentId == studentId);

                if (progress == null)
                {
                    progress = new StudentProgress
                    {
                        ClassId = classId,
                        NodeId = nodeId,
                        StudentId = studentId,
                        IsCompleted = isCompleted,
                        CompletedAt = isCompleted ? DateTime.Now : null
                    };
                    _db.StudentProgresses.Add(progress);
                }
                else
                {
                    progress.IsCompleted = isCompleted;
                    progress.CompletedAt = isCompleted ? DateTime.Now : null;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("Học sinh {StudentId} đặt node {NodeId} lớp {ClassId} thành {State}",
                    studentId, nodeId, classId, isCompleted ? "HOÀN THÀNH" : "CHƯA HOÀN THÀNH");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt trạng thái hoàn thành node {NodeId} cho học sinh {StudentId} trong lớp {ClassId}",
                    nodeId, studentId, classId);
                throw;
            }
        }
    }
}
