using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class StudentProgress
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int NodeId { get; set; }

    public int ClassId { get; set; }

    public bool? IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Node Node { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
