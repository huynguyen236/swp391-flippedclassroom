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

namespace Flipped_Classroom.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public IndexModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public List<User> Users { get; set; } = new List<User>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RoleFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string ActiveFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;

        public async Task<IActionResult> OnGetAsync()
        {
            // Loại trừ hoàn toàn tài khoản Admin ngay từ đầu
            var query = _context.Users
                                .AsNoTracking()
                                .Where(u => u.Role != "Admin");

            // Apply Search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(u => u.FirstName.ToLower().Contains(term) ||
                                         u.LastName.ToLower().Contains(term) ||
                                         (u.Email != null && u.Email.ToLower().Contains(term)) ||
                                         u.Username.ToLower().Contains(term));
            }

            // Apply Role Filter (Bỏ qua nếu chọn "Admin" vì đã lọc bỏ ở trên)
            if (!string.IsNullOrWhiteSpace(RoleFilter) && RoleFilter != "All")
            {
                query = query.Where(u => u.Role == RoleFilter);
            }

            // Apply Active Filter
            if (!string.IsNullOrWhiteSpace(ActiveFilter) && ActiveFilter != "All")
            {
                bool isActive = ActiveFilter == "Active";
                query = query.Where(u => u.IsActive == isActive);
            }

            // Count total matching items
            TotalItems = await query.CountAsync();

            // Calculate total pages
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
            if (TotalPages < 1) TotalPages = 1;

            // Clamp PageIndex
            if (PageIndex < 1) PageIndex = 1;
            if (PageIndex > TotalPages) PageIndex = TotalPages;

            // Fetch paginated results
            Users = await query
                .OrderByDescending(u => u.Id)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }
    }
}
