using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Flipped_Classroom.Services.Implementations
{
    public class CurriculumService : ICurriculumService
    {
        private readonly Swp391NihongoContext _db;
        private readonly ILogger<CurriculumService> _logger;

        public CurriculumService(Swp391NihongoContext db, ILogger<CurriculumService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Curriculum>> GetAllCurriculumsAsync()
        {
            try
            {
                return await _db.Curriculums
                    .Include(c => c.Manager)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách khung chương trình");
                return new List<Curriculum>();
            }
        }

        public async Task<Curriculum?> GetCurriculumByIdAsync(int id)
        {
            try
            {
                return await _db.Curriculums
                    .Include(c => c.Manager)
                    .Include(c => c.Nodes.OrderBy(n => n.NodeOrder).ThenBy(n => n.Id))
                        .ThenInclude(n => n.Materials)
                    .Include(c => c.Nodes.OrderBy(n => n.NodeOrder).ThenBy(n => n.Id))
                        .ThenInclude(n => n.Quizzes)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin khung chương trình với Id: {Id}", id);
                return null;
            }
        }

        public async Task<Curriculum> CreateCurriculumAsync(Curriculum curriculum)
        {
            try
            {
                curriculum.CreatedAt = DateTime.Now;
                _db.Curriculums.Add(curriculum);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Đã tạo mới khung chương trình: {CurriculumName} (ID: {Id})", curriculum.CurriculumName, curriculum.Id);
                return curriculum;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo mới khung chương trình: {CurriculumName}", curriculum.CurriculumName);
                throw;
            }
        }

        public async Task<Node> CreateNodeAsync(Node node)
        {
            try
            {
                // Calculate NodeOrder if not specified
                if (!node.NodeOrder.HasValue || node.NodeOrder == 0)
                {
                    int maxOrder = 0;
                        var orders = await _db.Nodes
                            .Where(n => n.CurriculumId == node.CurriculumId && n.ParentNodeId == node.ParentNodeId)
                            .Select(n => n.NodeOrder)
                            .ToListAsync();
                        maxOrder = orders.Any() ? orders.Max() ?? 0 : 0;
                    
                    node.NodeOrder = maxOrder + 1;
                }

                _db.Nodes.Add(node);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Đã tạo node mới: {Title} (ID: {Id})", node.Title, node.Id);
                return node;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo node mới: {Title}", node.Title);
                throw;
            }
        }

        public async Task<Material> AddMaterialAsync(Material material)
        {
            try
            {
                _db.Materials.Add(material);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Đã thêm học liệu mới: {Title} (ID: {Id}) cho Node: {NodeId}", material.Title, material.Id, material.NodeId);
                return material;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm học liệu mới: {Title}", material.Title);
                throw;
            }
        }

        public async Task<bool> DeleteNodeAsync(int nodeId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var rootNode = await _db.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId);
                if (rootNode == null) return false;

                // Gom toàn bộ node trong cây con (đệ quy) để xóa cả chương lẫn các bài học con.
                var nodeIds = await CollectDescendantNodeIdsAsync(nodeId);

                await DeleteNodesAndDependentsAsync(nodeIds);

                await transaction.CommitAsync();
                _logger.LogInformation("Đã xóa node ID: {NodeId} cùng {Count} node trong cây con", nodeId, nodeIds.Count);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xóa node ID: {NodeId}", nodeId);
                return false;
            }
        }

        public async Task<(bool Success, string? Error)> DeleteCurriculumAsync(int curriculumId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var curriculum = await _db.Curriculums.FirstOrDefaultAsync(c => c.Id == curriculumId);
                if (curriculum == null)
                    return (false, "Không tìm thấy khung chương trình.");

                // Chặn xóa nếu còn bất kỳ lớp nào gắn với khung này.
                // (Class.CurriculumId không cho phép null nên không thể gỡ liên kết tự động — buộc phải giữ dữ liệu lớp.)
                var totalClassCount = await _db.Classes.CountAsync(c => c.CurriculumId == curriculumId);
                if (totalClassCount > 0)
                {
                    var activeClassCount = await _db.Classes.CountAsync(c => c.CurriculumId == curriculumId && c.Status == "Active");
                    var message = activeClassCount > 0
                        ? $"Không thể xóa: có {activeClassCount} lớp đang học sử dụng khung chương trình này. Hãy kết thúc và gỡ các lớp trước."
                        : $"Không thể xóa: còn {totalClassCount} lớp gắn với khung chương trình này. Hãy gỡ các lớp trước.";
                    return (false, message);
                }

                // Gom tất cả node thuộc khung (mọi cấp) rồi xóa cùng dữ liệu phụ thuộc.
                var nodeIds = await _db.Nodes
                    .Where(n => n.CurriculumId == curriculumId)
                    .Select(n => n.Id)
                    .ToListAsync();

                if (nodeIds.Count > 0)
                    await DeleteNodesAndDependentsAsync(nodeIds);

                await _db.Curriculums.Where(c => c.Id == curriculumId).ExecuteDeleteAsync();

                await transaction.CommitAsync();
                _logger.LogInformation("Đã xóa khung chương trình ID: {Id} cùng {Count} node", curriculumId, nodeIds.Count);
                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xóa khung chương trình ID: {Id}", curriculumId);
                return (false, "Đã xảy ra lỗi khi xóa khung chương trình.");
            }
        }

        // Trả về id của node gốc và toàn bộ node con (đệ quy) qua BFS.
        private async Task<List<int>> CollectDescendantNodeIdsAsync(int rootNodeId)
        {
            var nodeIds = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(rootNodeId);
            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                nodeIds.Add(currentId);
                var childIds = await _db.Nodes
                    .Where(n => n.ParentNodeId == currentId)
                    .Select(n => n.Id)
                    .ToListAsync();
                foreach (var childId in childIds)
                    queue.Enqueue(childId);
            }
            return nodeIds;
        }

        // Xóa toàn bộ dữ liệu phụ thuộc của tập node rồi xóa chính các node.
        // KHÔNG tự quản transaction — caller chịu trách nhiệm bọc transaction.
        private async Task DeleteNodesAndDependentsAsync(List<int> nodeIds)
        {
            // Xóa từ lá lên gốc để không vi phạm ràng buộc khóa ngoại.

            // --- Nhánh Quiz ---
            var quizIds = await _db.Quizzes.Where(q => nodeIds.Contains(q.NodeId))
                .Select(q => q.Id).ToListAsync();
            if (quizIds.Count > 0)
            {
                var quizResultIds = await _db.QuizResults.Where(r => quizIds.Contains(r.QuizId))
                    .Select(r => r.Id).ToListAsync();
                await _db.QuizAnswers.Where(a => quizResultIds.Contains(a.QuizResultId)).ExecuteDeleteAsync();
                await _db.QuizResults.Where(r => quizIds.Contains(r.QuizId)).ExecuteDeleteAsync();
                await _db.QuizQuestions.Where(qq => quizIds.Contains(qq.QuizId)).ExecuteDeleteAsync();
                await _db.Quizzes.Where(q => quizIds.Contains(q.Id)).ExecuteDeleteAsync();
            }

            // --- Nhánh Question ---
            var questionIds = await _db.Questions.Where(q => nodeIds.Contains(q.NodeId))
                .Select(q => q.Id).ToListAsync();
            if (questionIds.Count > 0)
            {
                await _db.QuizAnswers.Where(a => questionIds.Contains(a.QuestionId)).ExecuteDeleteAsync();
                await _db.QuizQuestions.Where(qq => questionIds.Contains(qq.QuestionId)).ExecuteDeleteAsync();
                await _db.StudentMistakes.Where(m => questionIds.Contains(m.QuestionId)).ExecuteDeleteAsync();
                await _db.QuestionOptions.Where(o => questionIds.Contains(o.QuestionId)).ExecuteDeleteAsync();
                await _db.Questions.Where(q => questionIds.Contains(q.Id)).ExecuteDeleteAsync();
            }

            // --- Nhánh Assignment ---
            var assignmentIds = await _db.Assignments.Where(a => a.NodeId != null && nodeIds.Contains(a.NodeId.Value))
                .Select(a => a.Id).ToListAsync();
            if (assignmentIds.Count > 0)
            {
                var submissionIds = await _db.Submissions.Where(s => assignmentIds.Contains(s.AssignmentId))
                    .Select(s => s.Id).ToListAsync();
                await _db.FeedbackComments.Where(f => submissionIds.Contains(f.SubmissionId)).ExecuteDeleteAsync();
                await _db.Submissions.Where(s => assignmentIds.Contains(s.AssignmentId)).ExecuteDeleteAsync();
                await _db.Assignments.Where(a => assignmentIds.Contains(a.Id)).ExecuteDeleteAsync();
            }

            // --- Nhánh QaThread ---
            var qaThreadIds = await _db.QaThreads.Where(t => t.NodeId != null && nodeIds.Contains(t.NodeId.Value))
                .Select(t => t.Id).ToListAsync();
            if (qaThreadIds.Count > 0)
            {
                await _db.QaReplies.Where(r => qaThreadIds.Contains(r.QaThreadId)).ExecuteDeleteAsync();
                await _db.QaThreads.Where(t => qaThreadIds.Contains(t.Id)).ExecuteDeleteAsync();
            }

            // --- Các bảng trỏ thẳng về Node ---
            await _db.Materials.Where(m => nodeIds.Contains(m.NodeId)).ExecuteDeleteAsync();
            await _db.StudentProgresses.Where(p => nodeIds.Contains(p.NodeId)).ExecuteDeleteAsync();
            await _db.Vocabularies.Where(v => nodeIds.Contains(v.NodeId)).ExecuteDeleteAsync();
            await _db.ClassNodeStatuses.Where(c => nodeIds.Contains(c.NodeId)).ExecuteDeleteAsync();

            // --- Xóa Node con trước, node cha sau (tránh vi phạm FK_Node_Parent) ---
            // Sắp theo độ sâu giảm dần: node nào là cha của node khác trong tập sẽ bị xóa sau.
            var idSet = new HashSet<int>(nodeIds);
            var parentMap = await _db.Nodes
                .Where(n => nodeIds.Contains(n.Id))
                .Select(n => new { n.Id, n.ParentNodeId })
                .ToDictionaryAsync(n => n.Id, n => n.ParentNodeId);

            var depth = new Dictionary<int, int>();
            int DepthOf(int id)
            {
                if (depth.TryGetValue(id, out var d)) return d;
                var parent = parentMap.TryGetValue(id, out var p) ? p : null;
                d = (parent.HasValue && idSet.Contains(parent.Value)) ? DepthOf(parent.Value) + 1 : 0;
                depth[id] = d;
                return d;
            }

            foreach (var id in nodeIds.OrderByDescending(DepthOf))
                await _db.Nodes.Where(n => n.Id == id).ExecuteDeleteAsync();
        }

        public async Task<bool> DeleteMaterialAsync(int materialId)
        {
            try
            {
                var material = await _db.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
                if (material == null) return false;

                _db.Materials.Remove(material);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Đã xóa học liệu ID: {MaterialId}", materialId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa học liệu ID: {MaterialId}", materialId);
                return false;
            }
        }
    }
}
