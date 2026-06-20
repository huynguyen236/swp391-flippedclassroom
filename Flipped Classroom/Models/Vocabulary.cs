using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Vocabulary
{
    public int Id { get; set; }

    public int NodeId { get; set; }

    public string Word { get; set; } = null!;

    public string Hiragana { get; set; } = null!;

    public string Meaning { get; set; } = null!;

    public string? Romaji { get; set; }

    public int? DifficultyLevel { get; set; } = 1;

    public virtual Node Node { get; set; } = null!;
}
