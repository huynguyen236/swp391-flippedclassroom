using System;

namespace Flipped_Classroom.Models;

public partial class ClassNodeStatus
{
    public int ClassId { get; set; }

    public int NodeId { get; set; }

    public bool IsUnlocked { get; set; }

    public DateTime? UnlockedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Node Node { get; set; } = null!;
}
