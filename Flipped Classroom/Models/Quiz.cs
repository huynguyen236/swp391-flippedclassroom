using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Quiz
{
    public int Id { get; set; }

    public int NodeId { get; set; }

    public int? ClassId { get; set; }

    public bool IsAlwaysOpen { get; set; } 

    public string Title { get; set; } = null!;

    public int? DurationMinutes { get; set; }

    public string? Status { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Node Node { get; set; } = null!;

    public virtual Class? Class { get; set; }

    public virtual ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();

    public virtual ICollection<QuizResult> QuizResults { get; set; } = new List<QuizResult>();
}
