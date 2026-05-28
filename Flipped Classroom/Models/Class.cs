using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Class
{
    public int Id { get; set; }

    public string ClassName { get; set; } = null!;

    public int ManagerId { get; set; }

    public string? InviteCode { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<GradeCategory> GradeCategories { get; set; } = new List<GradeCategory>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual User Manager { get; set; } = null!;

    public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<QaThread> QaThreads { get; set; } = new List<QaThread>();

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } = new List<ClassSchedule>();
}
