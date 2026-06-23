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

    public int? Duration { get; set; }

    /// <summary>
    /// Câu mẫu tiếng Nhật để luyện nói. Chỉ dùng khi MaterialType == "speech".
    /// </summary>
    public string? SpeechTargetText { get; set; }

    /// <summary>
    /// Nghĩa tiếng Việt của câu mẫu, hiển thị dưới câu Nhật. Chỉ dùng khi MaterialType == "speech".
    /// </summary>
    public string? SpeechMeaning { get; set; }

    public virtual Node Node { get; set; } = null!;
}
