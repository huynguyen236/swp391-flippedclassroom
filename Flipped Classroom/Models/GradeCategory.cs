using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class GradeCategory
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int Weight { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
