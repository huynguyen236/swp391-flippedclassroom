using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class QaReply
{
    public int Id { get; set; }

    public int QaThreadId { get; set; }

    public int UserId { get; set; }

    public string ReplyText { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual QaThread QaThread { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
