using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class QuestionOption
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public string OptionContent { get; set; } = null!;

    public bool? IsCorrect { get; set; }

    public virtual Question Question { get; set; } = null!;
}
