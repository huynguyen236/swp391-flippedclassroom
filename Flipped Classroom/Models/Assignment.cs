using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Assignment
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int? NodeId { get; set; }

    public string Title { get; set; } = null!;

    public string? RequirementText { get; set; }

    public string Type { get; set; } = null!;

    public DateTime? DueDate { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Node? Node { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
