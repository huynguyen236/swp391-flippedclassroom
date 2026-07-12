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

namespace Flipped_Classroom.Pages.StudentClasses
{
    [Authorize(Roles = "Student")]
    public class IndexModel : PageModel
    {
        private readonly Swp391NihongoContext _context;

        public IndexModel(Swp391NihongoContext context)
        {
            _context = context;
        }

        public IList<Class> Class { get; set; } = default!;

        [BindProperty]
        public string InviteCode { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                if (_context.Classes != null)
                {
                    Class = await _context
                        .Classes.Include(c => c.Manager)
                        .Where(c => c.ClassMembers.Any(cm => cm.UserId == userId))
                        .ToListAsync();
                }
                return Page();
            }

            return RedirectToPage("/Authentication/Login");
        }

        public async Task<IActionResult> OnPostJoinClassAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Authentication/Login");
            }

            if (string.IsNullOrWhiteSpace(InviteCode))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã lớp học.";
                return RedirectToPage();
            }

            var cleanedCode = InviteCode.Trim();

            var targetClass = await _context.Classes.FirstOrDefaultAsync(c =>
                c.InviteCode == cleanedCode
            );

            if (targetClass == null)
            {
                TempData["ErrorMessage"] = "Mã lớp học không hợp lệ.";
                return RedirectToPage();
            }

            var isAlreadyMember = await _context.ClassMembers.AnyAsync(cm =>
                cm.ClassId == targetClass.Id && cm.UserId == userId
            );

            if (isAlreadyMember)
            {
                TempData["ErrorMessage"] = "Bạn đã tham gia lớp học này rồi.";
                return RedirectToPage();
            }

            var duplicateCurriculumClass = await _context.Classes
                .FirstOrDefaultAsync(c => c.CurriculumId == targetClass.CurriculumId && 
                                         c.ClassMembers.Any(cm => cm.UserId == userId));

            if (duplicateCurriculumClass != null)
            {
                TempData["ErrorMessage"] = $"Bạn không thể tham gia lớp này vì đã tham gia lớp '{duplicateCurriculumClass.ClassName}' có cùng khung chương trình.";
                return RedirectToPage();
            }

            var classMember = new ClassMember
            {
                ClassId = targetClass.Id,
                UserId = userId,
                JoinedAt = DateTime.Now,
                IsSupportTeam = false,
            };

            _context.ClassMembers.Add(classMember);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Tham gia lớp học '{targetClass.ClassName}' thành công!";
            return RedirectToPage();
        }
    }
}
