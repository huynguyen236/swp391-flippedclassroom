using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Flipped_Classroom.Models.Speech;
using Flipped_Classroom.Services.Interfaces;
using MeCab;

namespace Flipped_Classroom.Services.Implementation
{
    /// <summary>
    /// So khớp giọng đọc tiếng Nhật: chuẩn hóa cả câu mẫu lẫn câu nói về Hiragana (MeCab),
    /// rồi tính điểm Levenshtein và truy vết so khớp chi tiết từng ký tự.
    /// </summary>
    public class SpeechService : ISpeechService
    {
        public SpeechComparisonResult CompareSpeech(string targetText, string spokenText)
        {
            if (string.IsNullOrWhiteSpace(targetText))
            {
                return new SpeechComparisonResult
                {
                    Success = false,
                    Message = "Câu gốc không được để trống."
                };
            }

            // 1. Chuẩn hóa: bỏ dấu câu & khoảng trắng
            string cleanTarget = NormalizeText(targetText);
            string cleanSpoken = NormalizeText(spokenText ?? string.Empty);

            // 2. MeCab: chuyển về Hiragana
            string targetHiragana = ConvertToHiragana(cleanTarget);
            string spokenHiragana = ConvertToHiragana(cleanSpoken);

            // 3. Tính điểm Levenshtein
            int distance = GetLevenshteinDistance(targetHiragana, spokenHiragana);
            int maxLength = Math.Max(targetHiragana.Length, spokenHiragana.Length);
            int score = maxLength == 0
                ? 100
                : (int)Math.Round((1.0 - (double)distance / maxLength) * 100);

            // 4. Truy vết so khớp chi tiết
            var alignment = AlignStrings(targetHiragana, spokenHiragana);

            return new SpeechComparisonResult
            {
                Success = true,
                Score = score,
                TargetHiragana = targetHiragana,
                SpokenHiragana = spokenHiragana,
                Distance = distance,
                Alignment = alignment
            };
        }

        // ── Chuẩn hóa: xóa dấu câu JP/EN và khoảng trắng ──
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return Regex.Replace(text, @"[、。？！\s\.,!\?]", "");
        }

        // ── Kanji/Katakana → Hiragana bằng MeCab ──
        private string ConvertToHiragana(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // MeCab.DotNet (với <MeCabUseDefaultDictionary>True</...>) tự copy ipadic
            // vào thư mục "dic" cạnh assembly khi build.
            var dicPath = Path.Combine(AppContext.BaseDirectory, "dic");
            var param = new MeCabParam { DicDir = dicPath };
            var result = new StringBuilder();

            using (var tagger = MeCabTagger.Create(param))
            {
                foreach (var node in tagger.ParseToNodes(text))
                {
                    if (node.CharType > 0) // bỏ qua BOS/EOS
                    {
                        var features = node.Feature.Split(',');
                        // features[7] = âm đọc (Katakana) trong ipadic
                        string reading = features.Length > 7
                                         && !string.IsNullOrWhiteSpace(features[7])
                                         && features[7] != "*"
                            ? features[7]
                            : node.Surface; // fallback giữ nguyên bề mặt
                        result.Append(reading);
                    }
                }
            }

            return KatakanaToHiragana(result.ToString());
        }

        // ── Katakana → Hiragana (lệch Unicode 0x60) ──
        private static string KatakanaToHiragana(string katakana)
        {
            if (string.IsNullOrEmpty(katakana)) return string.Empty;
            var sb = new StringBuilder();
            foreach (char c in katakana)
            {
                if (c >= 'ァ' && c <= 'ヶ')
                    sb.Append((char)(c - 0x60));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        // ── Levenshtein distance ──
        private static int GetLevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
            if (string.IsNullOrEmpty(b)) return a?.Length ?? 0;

            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[a.Length, b.Length];
        }

        // ── Traceback so khớp chi tiết ──
        private static List<AlignmentToken> AlignStrings(string a, string b)
        {
            var tokens = new List<AlignmentToken>();
            a ??= string.Empty;
            b ??= string.Empty;

            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }

            int ci = a.Length, cj = b.Length;
            while (ci > 0 || cj > 0)
            {
                // Chéo → match / mismatch
                if (ci > 0 && cj > 0 &&
                    d[ci, cj] == d[ci - 1, cj - 1] + (a[ci - 1] == b[cj - 1] ? 0 : 1))
                {
                    tokens.Add(new AlignmentToken
                    {
                        Character = a[ci - 1].ToString(),
                        SpokenCharacter = b[cj - 1].ToString(),
                        Type = a[ci - 1] == b[cj - 1] ? "match" : "mismatch"
                    });
                    ci--; cj--;
                }
                // Lên → delete (bỏ quên âm)
                else if (ci > 0 && (cj == 0 || d[ci, cj] == d[ci - 1, cj] + 1))
                {
                    tokens.Add(new AlignmentToken
                    {
                        Character = a[ci - 1].ToString(),
                        Type = "delete"
                    });
                    ci--;
                }
                // Trái → insert (nói dư âm)
                else if (cj > 0 && (ci == 0 || d[ci, cj] == d[ci, cj - 1] + 1))
                {
                    tokens.Add(new AlignmentToken
                    {
                        SpokenCharacter = b[cj - 1].ToString(),
                        Type = "insert"
                    });
                    cj--;
                }
                else break;
            }

            tokens.Reverse();
            return tokens;
        }
    }
}
