using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Flipped_Classroom.Models;

public partial class Class
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên lớp học.")]
    [StringLength(100, ErrorMessage = "Tên lớp học không được vượt quá 100 ký tự.")]
    public string ClassName { get; set; } = null!;

    public int ManagerId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn khung chương trình cho lớp.")]
    public int CurriculumId { get; set; }

    public string? InviteCode { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual User Manager { get; set; } = null!;

    public virtual Curriculum Curriculum { get; set; } = null!;

    public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual ICollection<QaThread> QaThreads { get; set; } = new List<QaThread>();

    public virtual ICollection<ClassSchedule> ClassSchedules { get; set; } =
        new List<ClassSchedule>();
}
