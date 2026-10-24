using JiebaNet.Segmenter;

namespace Liss.TextLab.Core;

public enum SegmentMode { Accurate, Search, Full }
public sealed record TextToken(string Word, int Start, int End);
public sealed record WordFrequency(string Word, int Count);
public sealed record SegmentResult(IReadOnlyList<TextToken> Tokens, IReadOnlyList<WordFrequency> Frequencies, int Characters);

public sealed class SegmentService
{
    private readonly JiebaSegmenter _segmenter = new();
    private readonly object _gate = new();
    public SegmentResult Segment(string text, SegmentMode mode = SegmentMode.Accurate, bool hmm = true, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > 2_000_000) throw new ArgumentException("单次文本最多 200 万字符；大文件请分批处理。");
        var tokens = new List<TextToken>();
        lock (_gate)
        {
            // Sentence-sized chunks keep cancellation responsive while retaining normal context.
            int offset = 0;
            while (offset < text.Length)
            {
                cancellation.ThrowIfCancellationRequested();
                int length = Math.Min(4096, text.Length - offset);
                if (length < text.Length - offset)
                {
                    int stop = text.LastIndexOfAny(['\n', '。', '！', '？'], offset + length - 1, length);
                    if (stop >= offset) length = stop - offset + 1;
                    if (char.IsHighSurrogate(text[offset + length - 1])) length--;
                }
                string part = text.Substring(offset, length);
                if (mode == SegmentMode.Full)
                {
                    int cursor = 0;
                    foreach (var word in _segmenter.Cut(part, cutAll: true, hmm: false))
                    {
                        cancellation.ThrowIfCancellationRequested();
                        if (string.IsNullOrWhiteSpace(word)) continue;
                        int start = part.IndexOf(word, Math.Max(0, cursor - word.Length), StringComparison.Ordinal);
                        if (start < 0) start = part.IndexOf(word, StringComparison.Ordinal);
                        if (start < 0) continue;
                        tokens.Add(new(word, offset + start, offset + start + word.Length)); cursor = start;
                    }
                }
                else
                {
                    foreach (var token in _segmenter.Tokenize(part, mode == SegmentMode.Search ? TokenizerMode.Search : TokenizerMode.Default, hmm))
                    {
                        cancellation.ThrowIfCancellationRequested();
                        if (!string.IsNullOrWhiteSpace(token.Word)) tokens.Add(new(token.Word, offset + token.StartIndex, offset + token.EndIndex));
                    }
                }
                offset += length;
            }
        }
        var frequencies = tokens.Where(t => t.Word.Any(char.IsLetterOrDigit)).GroupBy(t => t.Word, StringComparer.Ordinal)
            .Select(g => new WordFrequency(g.Key, g.Count())).OrderByDescending(f => f.Count).ThenBy(f => f.Word, StringComparer.Ordinal).ToArray();
        return new(tokens, frequencies, text.Length);
    }
    public void AddWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length > 100 || word.Any(char.IsWhiteSpace)) throw new ArgumentException("自定义词须为 1–100 个非空白字符。");
        lock (_gate) _segmenter.AddWord(word);
    }
}
