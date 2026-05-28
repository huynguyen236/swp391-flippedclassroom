using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flipped_Classroom.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public DashboardModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int AdminCount { get; set; }
        public int ManagerCount { get; set; }
        public int TeacherCount { get; set; }
        public int StudentCount { get; set; }
        public int SupportCount { get; set; }
        public int ActiveClassesCount { get; set; }
        public List<User> NewestUsers { get; set; } = new List<User>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Total users
            TotalUsers = await _context.Users.CountAsync();

            // Total users by role
            AdminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
            ManagerCount = await _context.Users.CountAsync(u => u.Role == "Manager");
            TeacherCount = await _context.Users.CountAsync(u => u.Role == "Teacher");
            StudentCount = await _context.Users.CountAsync(u => u.Role == "Student");
            SupportCount = await _context.Users.CountAsync(u => u.Role == "Support");

            // Total active classes
            ActiveClassesCount = await _context.Classes.CountAsync(c => c.Status == "Active");

            // 5 newest users
            NewestUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(u => u.Id)
                .Take(5)
                .ToListAsync();

            return Page();
        }
    }
}
