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
                    if (node.CurriculumId.HasValue)
                    {
                        var orders = await _db.Nodes
                            .Where(n => n.CurriculumId == node.CurriculumId && n.ParentNodeId == node.ParentNodeId)
                            .Select(n => n.NodeOrder)
                            .ToListAsync();
                        maxOrder = orders.Any() ? orders.Max() ?? 0 : 0;
                    }
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
            try
            {
                var node = await _db.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId);
                if (node == null) return false;

                _db.Nodes.Remove(node);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Đã xóa node ID: {NodeId}", nodeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa node ID: {NodeId}", nodeId);
                return false;
            }
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
