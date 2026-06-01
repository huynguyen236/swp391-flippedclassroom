using System.Collections.Generic;
using System.Threading.Tasks;
using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface ICurriculumService
    {
        Task<List<Curriculum>> GetAllCurriculumsAsync();
        Task<Curriculum?> GetCurriculumByIdAsync(int id);
        Task<Curriculum> CreateCurriculumAsync(Curriculum curriculum);
        Task<Node> CreateNodeAsync(Node node);
        Task<Material> AddMaterialAsync(Material material);
        Task<bool> DeleteNodeAsync(int nodeId);
        Task<bool> DeleteMaterialAsync(int materialId);
    }
}
