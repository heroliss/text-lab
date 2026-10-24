using System.Text;

namespace Liss.TextLab.Core;

public enum TextEncodingChoice { Auto, Utf8, Gb18030, Utf16 }
public sealed record DecodedText(string Text, string Encoding);
public sealed record FileSearchOptions(string Root, string Keyword, string Extensions = ".txt,.md,.cs,.cpp,.h,.json,.csv,.log", bool IgnoreCase = true, TextEncodingChoice Encoding = TextEncodingChoice.Auto, long MaximumFileBytes = 64L * 1024 * 1024, int MaximumHits = 10_000);
public sealed record FileHit(string Path, int Line, string Snippet, string Encoding, int Occurrences);
public sealed record FileProgress(int Scanned, int MatchedFiles, int Hits, int Skipped, string Current);
public sealed record SearchSummary(IReadOnlyList<FileHit> Hits, int Scanned, int MatchedFiles, int Skipped, bool Cancelled, bool LimitReached, IReadOnlyList<string> Notes);
public sealed record BatchFileResult(string Source, string Output, string Encoding, int Tokens);

public static class TextFiles
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    static TextFiles() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    public static (Encoding Encoding, int BomBytes, string Name) Detect(string path, TextEncodingChoice choice = TextEncodingChoice.Auto, CancellationToken cancellation = default)
    {
        using var stream = File.OpenRead(path); byte[] sample = new byte[8192]; int count = stream.Read(sample);
        if (count >= 4 && sample[0] == 0xff && sample[1] == 0xfe && sample[2] == 0 && sample[3] == 0) return (new UTF32Encoding(false, true, true), 4, "UTF-32 LE");
        if (count >= 4 && sample[0] == 0 && sample[1] == 0 && sample[2] == 0xfe && sample[3] == 0xff) return (new UTF32Encoding(true, true, true), 4, "UTF-32 BE");
        if (count >= 3 && sample[0] == 0xef && sample[1] == 0xbb && sample[2] == 0xbf) return (StrictUtf8, 3, "UTF-8 BOM");
        if (count >= 2 && sample[0] == 0xff && sample[1] == 0xfe) return (new UnicodeEncoding(false, true, true), 2, "UTF-16 LE");
        if (count >= 2 && sample[0] == 0xfe && sample[1] == 0xff) return (new UnicodeEncoding(true, true, true), 2, "UTF-16 BE");
        if (choice != TextEncodingChoice.Auto) return choice switch
        {
            TextEncodingChoice.Utf8 => (StrictUtf8, 0, "UTF-8"), TextEncodingChoice.Gb18030 => (Encoding.GetEncoding(54936, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback), 0, "GB18030"),
            TextEncodingChoice.Utf16 => (new UnicodeEncoding(false, true, true), 0, "UTF-16 LE"), _ => throw new ArgumentOutOfRangeException(nameof(choice))
        };
        if (sample.AsSpan(0, count).Contains((byte)0)) throw new InvalidDataException("检测到二进制内容（无 BOM 且包含零字节）。");
        // Validate the complete UTF-8 byte stream before choosing a single decoding pass.
        stream.Position = 0; var decoder = StrictUtf8.GetDecoder(); var chars = new char[8192];
        try
        {
            while ((count = stream.Read(sample)) > 0) { cancellation.ThrowIfCancellationRequested(); decoder.GetChars(sample, 0, count, chars, 0, false); }
            decoder.GetChars([], 0, 0, chars, 0, true); return (StrictUtf8, 0, "UTF-8");
        }
        catch (DecoderFallbackException) { return (Encoding.GetEncoding(54936, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback), 0, "GB18030"); }
    }
    public static DecodedText Read(string path, TextEncodingChoice choice = TextEncodingChoice.Auto, long maxBytes = 64L * 1024 * 1024, CancellationToken cancellation = default)
    {
        if (new FileInfo(path).Length > maxBytes) throw new InvalidDataException($"文件超过 {maxBytes / 1024 / 1024} MiB 限制。");
        var detected = Detect(path, choice, cancellation); using var stream = File.OpenRead(path); stream.Position = detected.BomBytes;
        using var reader = new StreamReader(stream, detected.Encoding, detectEncodingFromByteOrderMarks: true); var sb = new StringBuilder(); var buffer = new char[8192]; int count;
        while ((count = reader.Read(buffer, 0, buffer.Length)) > 0) { cancellation.ThrowIfCancellationRequested(); sb.Append(buffer, 0, count); }
        return new(sb.ToString(), detected.Name);
    }
    public static IEnumerable<string> Enumerate(string root, string extensions, Action<string>? onSkipped = null, CancellationToken cancellation = default)
    {
        root = Path.GetFullPath(root); if (!Directory.Exists(root)) throw new DirectoryNotFoundException("搜索目录不存在。");
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) throw new IOException("请选择实际目录，不从符号链接根目录遍历。");
        var suffixes = extensions.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries).Select(e => e == "*" ? "*" : "." + e.TrimStart('*', '.')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.TryPop(out string? current))
        {
            cancellation.ThrowIfCancellationRequested(); string[] files; string[] directories;
            try { files = Directory.GetFiles(current); directories = Directory.GetDirectories(current); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { onSkipped?.Invoke(current + "：" + e.Message); continue; }
            foreach (string path in files.Order(StringComparer.OrdinalIgnoreCase))
            {
                cancellation.ThrowIfCancellationRequested(); if (!suffixes.Contains("*") && suffixes.Count > 0 && !suffixes.Contains(Path.GetExtension(path))) continue;
                bool reparse;
                try { reparse = (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { onSkipped?.Invoke(path + "：" + e.Message); continue; }
                if (reparse) { onSkipped?.Invoke(path + "：符号链接"); continue; } yield return path;
            }
            foreach (string directory in directories.OrderDescending(StringComparer.OrdinalIgnoreCase))
            {
                try { if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0) pending.Push(directory); else onSkipped?.Invoke(directory + "：符号链接"); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { onSkipped?.Invoke(directory + "：" + e.Message); }
            }
        }
    }
}

public static class FileSearcher
{
    public static SearchSummary Search(FileSearchOptions options, IProgress<FileProgress>? progress = null, CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(options.Keyword) || options.Keyword.Contains('\n') || options.Keyword.Contains('\r')) throw new ArgumentException("请输入单行非空关键词。");
        if (options.Keyword.Length > 4096 || options.MaximumFileBytes is < 1 or > 2L * 1024 * 1024 * 1024 || options.MaximumHits is < 1 or > 1_000_000) throw new ArgumentOutOfRangeException(nameof(options));
        var hits = new List<FileHit>(); var notes = new List<string>(); int scanned = 0, matched = 0, skipped = 0; bool cancelled = false, limit = false;
        void Note(string message) { skipped++; if (notes.Count < 100) notes.Add(message); }
        try
        {
            foreach (string path in TextFiles.Enumerate(options.Root, options.Extensions, Note, cancellation))
            {
                cancellation.ThrowIfCancellationRequested(); scanned++; progress?.Report(new(scanned, matched, hits.Count, skipped, path));
                try
                {
                    if (new FileInfo(path).Length > options.MaximumFileBytes) { Note(path + "：超过单文件大小限制"); continue; }
                    var detected = TextFiles.Detect(path, options.Encoding, cancellation); using var stream = File.OpenRead(path); stream.Position = detected.BomBytes;
                    using var reader = new StreamReader(stream, detected.Encoding, true); bool hasMatch = false;
                    foreach (var line in MatchingLines(reader, options.Keyword, options.IgnoreCase, cancellation))
                    {
                        hits.Add(new(path, line.Line, line.Snippet, detected.Name, line.Occurrences)); if (!hasMatch) { matched++; hasMatch = true; }
                        if (hits.Count >= options.MaximumHits) { limit = true; break; }
                    }
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or DecoderFallbackException or InvalidDataException) { Note(path + "：" + e.Message); }
                if (limit) break;
            }
        }
        catch (OperationCanceledException) { cancelled = true; }
        progress?.Report(new(scanned, matched, hits.Count, skipped, cancelled ? "已取消，保留已找到的结果" : "搜索完成"));
        return new(hits, scanned, matched, skipped, cancelled, limit, notes);
    }
    private sealed record MatchingLine(int Line, string Snippet, int Occurrences);
    private static IEnumerable<MatchingLine> MatchingLines(StreamReader reader, string keyword, bool ignoreCase, CancellationToken cancellation)
    {
        // KMP spans read buffers without ever retaining a complete physical line.
        char Fold(char c) => ignoreCase ? char.ToUpperInvariant(c) : c;
        char[] pattern = keyword.Select(Fold).ToArray(); var prefix = new int[pattern.Length];
        for (int i = 1, j = 0; i < pattern.Length; i++) { while (j > 0 && pattern[i] != pattern[j]) j = prefix[j - 1]; if (pattern[i] == pattern[j]) j++; prefix[i] = j; }
        var buffer = new char[8192]; var recent = new Queue<char>(keyword.Length + 70); var snippet = new StringBuilder(240);
        int line = 1, position = 0, partial = 0, occurrences = 0, captureStart = 0; bool previousCr = false; int read;
        MatchingLine? Finish()
        {
            MatchingLine? result = occurrences == 0 ? null : new(line, (captureStart > 0 ? "…" : "") + snippet + (captureStart + snippet.Length < position ? "…" : ""), occurrences);
            position = partial = occurrences = captureStart = 0; snippet.Clear(); recent.Clear(); line++; return result;
        }
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellation.ThrowIfCancellationRequested();
            for (int offset = 0; offset < read; offset++)
            {
                if (offset % 1024 == 0) cancellation.ThrowIfCancellationRequested(); char c = buffer[offset];
                if (c is '\r' or '\n')
                {
                    if (c == '\n' && previousCr) { previousCr = false; continue; }
                    previousCr = c == '\r'; var match = Finish(); if (match is not null) yield return match; continue;
                }
                previousCr = false; position++;
                if (occurrences > 0) { if (snippet.Length < 240) snippet.Append(c); }
                else { recent.Enqueue(c); if (recent.Count > keyword.Length + 70) recent.Dequeue(); }
                char folded = Fold(c); while (partial > 0 && folded != pattern[partial]) partial = prefix[partial - 1]; if (folded == pattern[partial]) partial++;
                if (partial != pattern.Length) continue;
                partial = 0; occurrences++;
                if (occurrences == 1)
                {
                    captureStart = Math.Max(0, position - keyword.Length - 70); foreach (char value in recent.Take(240)) snippet.Append(value); recent.Clear();
                }
            }
        }
        if (position > 0) { var match = Finish(); if (match is not null) yield return match; }
    }
}

public static class BatchSegmenter
{
    public static IReadOnlyList<BatchFileResult> Run(SegmentService segmenter, string sourceRoot, string outputRoot, SegmentMode mode = SegmentMode.Accurate,
        TextEncodingChoice encoding = TextEncodingChoice.Auto, IProgress<FileProgress>? progress = null, CancellationToken cancellation = default)
    {
        sourceRoot = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        outputRoot = Path.GetFullPath(outputRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (outputRoot.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase) || sourceRoot.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("输入和输出目录须相互独立，避免覆盖原文或再次读取生成文件。");
        var files = TextFiles.Enumerate(sourceRoot, ".txt,.md,.csv", cancellation: cancellation).ToArray(); var results = new List<BatchFileResult>();
        foreach (string file in files)
        {
            cancellation.ThrowIfCancellationRequested(); var text = TextFiles.Read(file, encoding, 8L * 1024 * 1024, cancellation); var segmented = segmenter.Segment(text.Text, mode, cancellation: cancellation);
            string output = Path.Combine(outputRoot, Path.GetRelativePath(sourceRoot, file) + ".segmented.txt"); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            string temporary = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllText(temporary, string.Join(" / ", segmented.Tokens.Select(t => t.Word)), new UTF8Encoding(false)); cancellation.ThrowIfCancellationRequested(); File.Move(temporary, output, overwrite: true); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            results.Add(new(file, output, text.Encoding, segmented.Tokens.Count)); progress?.Report(new(results.Count, files.Length, segmented.Tokens.Count, 0, file));
        }
        return results;
    }
}
