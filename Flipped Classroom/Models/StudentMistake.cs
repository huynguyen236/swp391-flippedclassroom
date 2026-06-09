using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class StudentMistake
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int QuestionId { get; set; }

    public int? ErrorCount { get; set; }

    public string? MistakeType { get; set; }

    public DateOnly? NextReviewDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsResolved { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
