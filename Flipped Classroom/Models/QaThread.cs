using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class QaThread
{
    public int Id { get; set; }

    public int? NodeId { get; set; }

    public int? ClassId { get; set; }

    public int? StudentId { get; set; }

    public string QuestionText { get; set; } = null!;

    public int? UpvoteCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Class? Class { get; set; }

    public virtual Node? Node { get; set; }

    public virtual ICollection<QaReply> QaReplies { get; set; } = new List<QaReply>();

    public virtual User? Student { get; set; }
}
