using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? Email { get; set; }

    public string? GoogleId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Gender { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<DailyReviewLog> DailyReviewLogs { get; set; } = new List<DailyReviewLog>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<FeedbackComment> FeedbackComments { get; set; } = new List<FeedbackComment>();

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual ICollection<QaReply> QaReplies { get; set; } = new List<QaReply>();

    public virtual ICollection<QaThread> QaThreads { get; set; } = new List<QaThread>();

    public virtual ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();

    public virtual ICollection<StudentMistake> StudentMistakes { get; set; } = new List<StudentMistake>();

    public virtual ICollection<StudentProgress> StudentProgresses { get; set; } = new List<StudentProgress>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<Curriculum> Curriculums { get; set; } = new List<Curriculum>();
}
