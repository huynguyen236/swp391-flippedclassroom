using Flipped_Classroom.Data;
using Flipped_Classroom.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Flipped_Classroom.Pages.Authentication
{
    public class RegisterModel : PageModel
    {
        private readonly FlippedClassroomContext _context;

        public RegisterModel(FlippedClassroomContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required(ErrorMessage = "Full Name is required.")]
            [StringLength(150, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Username is required.")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(256, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == Input.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Input.Username", "Username is already taken.");
                    return Page();
                }

                // Create new user
                var user = new User
                {
                    FullName = Input.FullName,
                    Email = Input.Email,
                    Username = Input.Username,
                    Password = Input.Password, // Simple insert as requested. In production, use hashed passwords.
                    Role = "Student", // Default role
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Redirect to login with a success message (optional: use TempData)
                TempData["SuccessMessage"] = "Registration successful! You can now log in.";
                return RedirectToPage("/Authentication/Login");
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
