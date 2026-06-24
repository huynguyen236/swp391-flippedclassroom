using System.Collections.Generic;
using Flipped_Classroom.Models.Speech;

namespace Flipped_Classroom.Services.Interfaces
{
    public interface ISpeechService
    {
        SpeechComparisonResult CompareSpeech(string targetText, string spokenText);
    }

    public class SpeechComparisonResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Score { get; set; }
        public string TargetHiragana { get; set; } = string.Empty;
        public string SpokenHiragana { get; set; } = string.Empty;
        public int Distance { get; set; }
        public List<AlignmentToken> Alignment { get; set; } = new();
    }
}
