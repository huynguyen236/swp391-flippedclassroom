using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class ClassSchedule
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public DateOnly StudyDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Room { get; set; }

    public virtual Class Class { get; set; } = null!;
}
