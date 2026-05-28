using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Milestone
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string Title { get; set; } = null!;

    public DateTime? Deadline { get; set; }

    public string? Description { get; set; }

    public virtual Project Project { get; set; } = null!;
}
