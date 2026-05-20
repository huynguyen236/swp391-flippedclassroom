using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class QuizResult
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int QuizId { get; set; }

    public decimal Score { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
