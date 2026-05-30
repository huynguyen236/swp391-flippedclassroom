using System;

namespace Flipped_Classroom.Models;

public partial class DailyReviewLog
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public DateOnly ReviewDate { get; set; }

    public int ReviewedCount { get; set; }

    public int CorrectCount { get; set; }

    public int MasteredCount { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual User Student { get; set; } = null!;
}
