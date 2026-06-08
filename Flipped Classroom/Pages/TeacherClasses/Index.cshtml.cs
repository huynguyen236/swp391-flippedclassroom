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

namespace Flipped_Classroom.Pages.TeacherClasses
{
    [Authorize(Roles = "Teacher")]
    public class IndexModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public IndexModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public IList<Class> Class { get; set; } = default!;

        // Dashboard KPI
        public int TotalClassesCount { get; set; }
        public int TotalStudentsAll { get; set; }
        public int TotalAssignmentsAll { get; set; }

        // Per-class: classId -> avg progress %
        public Dictionary<int, double> ClassProgressMap { get; set; } = new();

        // Per-class: classId -> (submitted count, total possible)
        public Dictionary<int, (int Submitted, int Total)> ClassSubmissionMap { get; set; } = new();

        // Per-class: classId -> list of student names who have NOT submitted at least one assignment
        public Dictionary<int, List<string>> ClassPendingStudents { get; set; } = new();

        // Per-class: classId -> student count
        public Dictionary<int, int> ClassStudentCount { get; set; } = new();

        // Per-class: classId -> lesson count
        public Dictionary<int, int> ClassLessonCount { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            if (_context.Classes == null)
            {
                Class = new List<Class>();
                return Page();
            }

            // Load classes with related data
            Class = await _context.Classes
                .Include(c => c.Manager)
                .Include(c => c.ClassMembers)
                    .ThenInclude(cm => cm.User)
                .Include(c => c.Curriculum)
                    .ThenInclude(cu => cu!.Nodes)
                .Include(c => c.Assignments)
                    .ThenInclude(a => a.Submissions)
                        .ThenInclude(s => s.Student)
                .Where(c => c.ManagerId == userId)
                .ToListAsync();

            TotalClassesCount = Class.Count;

            // Load all student progress records for these classes in one query
            var classIds = Class.Select(c => c.Id).ToList();
            var allProgress = await _context.StudentProgresses
                .AsNoTracking()
                .Where(p => classIds.Contains(p.ClassId) && p.IsCompleted == true)
                .Select(p => new { p.ClassId, p.StudentId, p.NodeId })
                .ToListAsync();

            var progressByClass = allProgress
                .GroupBy(p => p.ClassId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var cls in Class)
            {
                var studentCount = cls.ClassMembers?.Count ?? 0;
                ClassStudentCount[cls.Id] = studentCount;
                TotalStudentsAll += studentCount;

                // Lesson count (nodes with a parent = leaf lessons)
                var lessonNodes = cls.Curriculum?.Nodes?
                    .Where(n => n.ParentNodeId != null)
                    .ToList() ?? new List<Node>();
                var lessonCount = lessonNodes.Count;
                ClassLessonCount[cls.Id] = lessonCount;

                // Progress calculation
                if (studentCount > 0 && lessonCount > 0)
                {
                    var totalPossible = studentCount * lessonCount;
                    var lessonNodeIds = lessonNodes.Select(n => n.Id).ToHashSet();
                    var completedCount = 0;
                    if (progressByClass.TryGetValue(cls.Id, out var classProgress))
                    {
                        completedCount = classProgress.Count(p => lessonNodeIds.Contains(p.NodeId));
                    }
                    ClassProgressMap[cls.Id] = Math.Round((double)completedCount * 100 / totalPossible, 1);
                }
                else
                {
                    ClassProgressMap[cls.Id] = 0;
                }

                // Submission stats
                var assignments = cls.Assignments?.ToList() ?? new List<Assignment>();
                var assignmentCount = assignments.Count;
                TotalAssignmentsAll += assignmentCount;

                var totalPossibleSubmissions = assignmentCount * studentCount;
                var allSubmissions = assignments
                    .SelectMany(a => a.Submissions ?? new List<Submission>())
                    .ToList();
                var actualSubmissions = allSubmissions.Count;

                ClassSubmissionMap[cls.Id] = (actualSubmissions, totalPossibleSubmissions);

                // Find students who haven't submitted at least one assignment
                var pendingNames = new List<string>();
                if (cls.ClassMembers != null && assignmentCount > 0)
                {
                    foreach (var member in cls.ClassMembers)
                    {
                        var studentSubmittedAssignmentIds = allSubmissions
                            .Where(s => s.StudentId == member.UserId)
                            .Select(s => s.AssignmentId)
                            .ToHashSet();

                        var hasUnsubmitted = assignments.Any(a => !studentSubmittedAssignmentIds.Contains(a.Id));
                        if (hasUnsubmitted)
                        {
                            pendingNames.Add(member.User?.Username ?? "Unknown");
                        }
                    }
                }
                ClassPendingStudents[cls.Id] = pendingNames;
            }

            return Page();
        }
    }
}
