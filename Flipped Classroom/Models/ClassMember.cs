using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class ClassMember
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int UserId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public bool? IsSupportTeam { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
