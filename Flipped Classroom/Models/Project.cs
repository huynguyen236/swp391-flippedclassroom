using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Project
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
}
