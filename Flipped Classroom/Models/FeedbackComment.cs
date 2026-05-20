using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class FeedbackComment
{
    public int Id { get; set; }

    public int SubmissionId { get; set; }

    public int ReviewerId { get; set; }

    public string CommentText { get; set; } = null!;

    public string? TimelineStamp { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Reviewer { get; set; } = null!;

    public virtual Submission Submission { get; set; } = null!;
}
