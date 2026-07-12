using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Flipped_Classroom.Pages.Classrooms
{
    [Authorize(Roles = "Admin,Manager")]
    public class DeleteModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;

        public DeleteModel(Flipped_Classroom.Data.Swp391NihongoContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Class Class { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Classes == null)
            {
                return NotFound();
            }

            var classroom = await _context.Classes.Include(c => c.Manager).FirstOrDefaultAsync(m => m.Id == id);

            if (classroom == null)
            {
                return NotFound();
            }
            else 
            {
                Class = classroom;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null || _context.Classes == null)
            {
                return NotFound();
            }
            var classroom = await _context.Classes.FindAsync(id);

            if (classroom != null)
            {
                int classId = classroom.Id;

                // 1. Delete QA Replies & QA Threads
                var qaThreads = await _context.QaThreads.Where(t => t.ClassId == classId).ToListAsync();
                if (qaThreads.Any())
                {
                    var qaThreadIds = qaThreads.Select(t => t.Id).ToList();
                    var qaReplies = await _context.QaReplies.Where(r => qaThreadIds.Contains(r.QaThreadId)).ToListAsync();
                    _context.QaReplies.RemoveRange(qaReplies);
                    _context.QaThreads.RemoveRange(qaThreads);
                }

                // 2. Delete Submissions & Assignments
                var assignments = await _context.Assignments.Where(a => a.ClassId == classId).ToListAsync();
                if (assignments.Any())
                {
                    var assignmentIds = assignments.Select(a => a.Id).ToList();
                    var submissions = await _context.Submissions.Where(s => assignmentIds.Contains(s.AssignmentId)).ToListAsync();
                    _context.Submissions.RemoveRange(submissions);
                    _context.Assignments.RemoveRange(assignments);
                }

                // 3. Delete QuizAnswers, QuizQuestions, QuizResults, Quizzes
                var quizzes = await _context.Quizzes.Where(q => q.ClassId == classId).ToListAsync();
                var quizIds = quizzes.Select(q => q.Id).ToList();

                var quizResults = await _context.QuizResults
                    .Where(r => r.ClassId == classId || quizIds.Contains(r.QuizId))
                    .ToListAsync();
                
                if (quizResults.Any())
                {
                    var resultIds = quizResults.Select(r => r.Id).ToList();
                    var quizAnswers = await _context.QuizAnswers.Where(a => resultIds.Contains(a.QuizResultId)).ToListAsync();
                    _context.QuizAnswers.RemoveRange(quizAnswers);
                    _context.QuizResults.RemoveRange(quizResults);
                }

                if (quizzes.Any())
                {
                    var quizQuestions = await _context.QuizQuestions.Where(qq => quizIds.Contains(qq.QuizId)).ToListAsync();
                    _context.QuizQuestions.RemoveRange(quizQuestions);
                    _context.Quizzes.RemoveRange(quizzes);
                }

                // 4. Delete GroupMembers & Groups
                var groups = await _context.Groups.Where(g => g.ClassId == classId).ToListAsync();
                if (groups.Any())
                {
                    var groupIds = groups.Select(g => g.Id).ToList();
                    var groupMembers = await _context.GroupMembers.Where(gm => groupIds.Contains(gm.GroupId)).ToListAsync();
                    _context.GroupMembers.RemoveRange(groupMembers);
                    _context.Groups.RemoveRange(groups);
                }

                // 5. Delete ClassMembers
                var classMembers = await _context.ClassMembers.Where(cm => cm.ClassId == classId).ToListAsync();
                _context.ClassMembers.RemoveRange(classMembers);

                // 6. Delete ClassSchedules
                var classSchedules = await _context.ClassSchedules.Where(cs => cs.ClassId == classId).ToListAsync();
                _context.ClassSchedules.RemoveRange(classSchedules);

                Class = classroom;
                _context.Classes.Remove(Class);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
