using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Question
{
    public int Id { get; set; }

    public int NodeId { get; set; }

    public string Content { get; set; } = null!;

    public string QuestionType { get; set; } = null!;

    public string? Visibility { get; set; }

    public bool? IsQuestionBank { get; set; }

    public string? CorrectAnswer { get; set; }

    public string? Explanation { get; set; }

    public int? DifficultyLevel { get; set; }

    public virtual Node Node { get; set; } = null!;

    public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();

    public virtual ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();

    public virtual ICollection<StudentMistake> StudentMistakes { get; set; } = new List<StudentMistake>();
}
