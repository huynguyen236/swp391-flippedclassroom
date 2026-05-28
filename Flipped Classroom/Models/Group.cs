using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Group
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public string GroupName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
