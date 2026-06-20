using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Flipped_Classroom.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Services.Implementation
{
    public class VocabularyService : IVocabularyService
    {
        private readonly Swp391NihongoContext _context;

        public VocabularyService(Swp391NihongoContext context)
        {
            _context = context;
        }

        public async Task<List<Vocabulary>> GetVocabulariesByNodeAsync(int nodeId)
        {
            return await _context.Vocabularies
                .Where(v => v.NodeId == nodeId)
                .ToListAsync();
        }

        public async Task<Vocabulary?> GetVocabularyByIdAsync(int id)
        {
            return await _context.Vocabularies.FindAsync(id);
        }

        public async Task<Vocabulary> CreateVocabularyAsync(Vocabulary vocab)
        {
            _context.Vocabularies.Add(vocab);
            await _context.SaveChangesAsync();
            return vocab;
        }

        public async Task<Vocabulary> UpdateVocabularyAsync(Vocabulary vocab)
        {
            _context.Entry(vocab).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return vocab;
        }

        public async Task<bool> DeleteVocabularyAsync(int id)
        {
            var vocab = await _context.Vocabularies.FindAsync(id);
            if (vocab == null) return false;

            _context.Vocabularies.Remove(vocab);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
