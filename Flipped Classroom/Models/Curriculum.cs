using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Curriculum
{
    public int Id { get; set; }

    public string CurriculumName { get; set; } = null!;

    public int ManagerId { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Manager { get; set; } = null!;

    public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
