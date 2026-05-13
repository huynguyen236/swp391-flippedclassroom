using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string? Password { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? GoogleId { get; set; }

    public string Role { get; set; } = null!;

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    public DateTime CreatedAt { get; set; }
}
