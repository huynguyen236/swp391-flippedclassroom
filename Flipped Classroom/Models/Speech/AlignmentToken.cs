namespace Flipped_Classroom.Models.Speech
{
    /// <summary>
    /// Một ký tự Hiragana sau khi so khớp giữa câu mẫu và câu học sinh nói.
    /// </summary>
    public class AlignmentToken
    {
        /// <summary>Ký tự trong câu mẫu (target). Rỗng nếu là 'insert'.</summary>
        public string Character { get; set; } = string.Empty;

        /// <summary>Ký tự học sinh thực sự nói (spoken). Rỗng nếu là 'delete'.</summary>
        public string SpokenCharacter { get; set; } = string.Empty;

        /// <summary>match | mismatch | delete | insert</summary>
        public string Type { get; set; } = string.Empty;
    }
}
