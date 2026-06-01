using System;

namespace Flipped_Classroom.Models;

public partial class QuizAnswer
{
    public int Id { get; set; }

    public int QuizResultId { get; set; }

    public int QuestionId { get; set; }

    public int? SelectedOptionId { get; set; }

    public string? AnswerText { get; set; }

    public bool IsCorrect { get; set; }

    public decimal? PointEarned { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual QuizResult QuizResult { get; set; } = null!;

    public virtual QuestionOption? SelectedOption { get; set; }
}
