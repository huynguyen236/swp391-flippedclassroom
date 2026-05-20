using System;
using System.Collections.Generic;

namespace Flipped_Classroom.Models;

public partial class Material
{
    public int Id { get; set; }

    public int NodeId { get; set; }

    public string Title { get; set; } = null!;

    public string MaterialType { get; set; } = null!;

    public string Url { get; set; } = null!;

    public long? FileSize { get; set; }

    public int? Duration { get; set; }

    public virtual Node Node { get; set; } = null!;
}
