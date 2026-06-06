using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Node
{
    public int Id { get; set; }

    public int? ClassId { get; set; }

    public int CurriculumId { get; set; }

    public int? ParentNodeId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? NodeOrder { get; set; }

    public string? Status { get; set; }

    public bool? IsReviewNode { get; set; }

    public bool? IsActive { get; set; }

    public int? EstimatedMinutes { get; set; }

    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public virtual Class? Class { get; set; }

    public virtual Curriculum Curriculum { get; set; } = null!;

    public virtual ICollection<Node> InverseParentNode { get; set; } = new List<Node>();

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    public virtual Node? ParentNode { get; set; }

    public virtual ICollection<QaThread> QaThreads { get; set; } = new List<QaThread>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual ICollection<StudentProgress> StudentProgresses { get; set; } = new List<StudentProgress>();
}
