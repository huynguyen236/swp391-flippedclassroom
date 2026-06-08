using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Flipped_Classroom.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToPage("/Admin/Dashboard");
                }
                if (User.IsInRole("Manager"))
                {
                    return RedirectToPage("/Classrooms/Index");
                }
                if (User.IsInRole("Teacher"))
                {
                    return RedirectToPage("/TeacherClasses/Index");
                }
                if (User.IsInRole("Student"))
                {
                    return RedirectToPage("/StudentClasses/Index");
                }
            }
            return Page();
        }
    }
}
