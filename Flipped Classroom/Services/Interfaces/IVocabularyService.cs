using System.Collections.Generic;
using System.Threading.Tasks;
using Flipped_Classroom.Models;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface IVocabularyService
    {
        Task<List<Vocabulary>> GetVocabulariesByNodeAsync(int nodeId);
        Task<Vocabulary?> GetVocabularyByIdAsync(int id);
        Task<Vocabulary> CreateVocabularyAsync(Vocabulary vocab);
        Task<Vocabulary> UpdateVocabularyAsync(Vocabulary vocab);
        Task<bool> DeleteVocabularyAsync(int id);
    }
}
