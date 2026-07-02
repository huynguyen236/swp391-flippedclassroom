using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Flipped_Classroom.Pages.Api
{
    [Authorize]
    public class VocabulariesModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public VocabulariesModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int? nodeId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return new JsonResult(new { success = false, message = "Unauthorized" }) { StatusCode = 401 };
            }

            List<Vocabulary> vocabList = new();

            if (nodeId.HasValue)
            {
                // 1. Fetch vocabularies for a specific lesson/node
                var node = await _context.Nodes
                    .Include(n => n.Vocabularies)
                    .FirstOrDefaultAsync(n => n.Id == nodeId.Value);

                if (node == null)
                {
                    return new JsonResult(new { success = false, message = "Lesson not found" }) { StatusCode = 404 };
                }

                vocabList = node.Vocabularies.ToList();
            }
            else
            {
                // 2. Fetch general review vocabularies (e.g. up to 30 random vocabs from user's curriculum)
                if (User.IsInRole("Admin"))
                {
                    vocabList = await _context.Vocabularies
                        .OrderBy(v => Guid.NewGuid())
                        .Take(30)
                        .ToListAsync();
                }
                else if (User.IsInRole("Teacher"))
                {
                    // Vocabs from curriculums managed by this teacher
                    var managedCurriculumIds = await _context.Classes
                        .Where(c => c.ManagerId == userId)
                        .Select(c => c.CurriculumId)
                        .Distinct()
                        .ToListAsync();

                    vocabList = await _context.Vocabularies
                        .Where(v => managedCurriculumIds.Contains(v.Node.CurriculumId))
                        .OrderBy(v => Guid.NewGuid())
                        .Take(30)
                        .ToListAsync();
                }
                else // Student or other
                {
                    // Vocabs from curriculums of classes enrolled
                    var enrolledCurriculumIds = await _context.ClassMembers
                        .Where(cm => cm.UserId == userId)
                        .Select(cm => cm.Class.CurriculumId)
                        .Distinct()
                        .ToListAsync();

                    vocabList = await _context.Vocabularies
                        .Where(v => enrolledCurriculumIds.Contains(v.Node.CurriculumId))
                        .OrderBy(v => Guid.NewGuid())
                        .Take(30)
                        .ToListAsync();
                }

                // Fallback: If user has no classes or classes have no vocabularies, just load any 30 random vocabularies
                if (!vocabList.Any())
                {
                    vocabList = await _context.Vocabularies
                        .OrderBy(v => Guid.NewGuid())
                        .Take(30)
                        .ToListAsync();
                }
            }

            var result = vocabList.Select(v => new
            {
                v.Id,
                v.Word,
                v.Hiragana,
                v.Meaning,
                v.Romaji,
                v.DifficultyLevel
            }).ToList();

            return new JsonResult(result);
        }
    }
}
