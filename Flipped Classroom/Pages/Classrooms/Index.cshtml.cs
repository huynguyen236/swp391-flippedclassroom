using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Authorization;

namespace Flipped_Classroom.Pages.Classrooms
{
    [Authorize(Roles = "Admin,Manager")]
    public class IndexModel : PageModel
    {
        private readonly Flipped_Classroom.Data.Swp391NihongoContext _context;

        public IndexModel(Flipped_Classroom.Data.Swp391NihongoContext context)
        {
            _context = context;
        }

        public IList<Class> Class { get;set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public async Task OnGetAsync()
        {
            if (_context.Classes != null)
            {
                var classesQuery = _context.Classes.Include(c => c.Manager).AsQueryable();

                if (!string.IsNullOrEmpty(SearchString))
                {
                    classesQuery = classesQuery.Where(c => c.ClassName.Contains(SearchString));
                }

                // Pagination
                int pageSize = 10;
                var count = await classesQuery.CountAsync();
                TotalPages = (int)System.Math.Ceiling(count / (double)pageSize);

                // Ensure PageIndex is within boundaries
                if (PageIndex < 1) PageIndex = 1;
                if (PageIndex > TotalPages && TotalPages > 0) PageIndex = TotalPages;

                Class = await classesQuery
                    .Skip((PageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
        }
    }
}
