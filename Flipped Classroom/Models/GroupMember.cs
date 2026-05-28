using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class GroupMember
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public int StudentId { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
