using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Grade
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int CategoryId { get; set; }

    public decimal? Score { get; set; }

    public string? Note { get; set; }

    public int? GradedBy { get; set; }

    public DateTime? GradedAt { get; set; }

    public virtual GradeCategory Category { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
