using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Submission
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }

    public int StudentId { get; set; }

    public int? GroupId { get; set; }

    public string? ContentText { get; set; }

    public string? MediaUrl { get; set; }

    public decimal? Score { get; set; }

    public string? Feedback { get; set; }

    public string? Status { get; set; }

    public DateTime? SubmitAt { get; set; }

    public virtual Assignment Assignment { get; set; } = null!;

    public virtual ICollection<FeedbackComment> FeedbackComments { get; set; } = new List<FeedbackComment>();

    public virtual Group? Group { get; set; }

    public virtual User Student { get; set; } = null!;
}
