using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Liss.TextLab.Core;

public sealed record SimilarWord(string Word, double Similarity);
public sealed record VectorProgress(int Loaded, int Total);

/// <summary>Word2vec binary/text importer and normalized cosine search, inspired by the 2016 implementation.</summary>
public sealed class WordVectors
{
    private readonly Dictionary<string, float[]> _vectors;
    public int Dimensions { get; }
    public int Count => _vectors.Count;
    private WordVectors(Dictionary<string, float[]> vectors, int dimensions) { _vectors = vectors; Dimensions = dimensions; }
    public bool Contains(string word) => _vectors.ContainsKey(word);
    public static WordVectors Load(string path, CancellationToken cancellation = default, IProgress<VectorProgress>? progress = null)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > 512L * 1024 * 1024) throw new InvalidDataException("模型文件最多 512 MiB，请选择较小词表。");
        return Path.GetExtension(path).Equals(".bin", StringComparison.OrdinalIgnoreCase)
            ? ReadBinary(stream, cancellation, progress) : ReadText(stream, cancellation, progress);
    }
    private static (int Count, int Dimensions) Header(string header)
    {
        var parts = header.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out int count) || !int.TryParse(parts[1], out int dimensions)
            || count < 1 || count > 2_000_000 || dimensions < 1 || dimensions > 4096 || (long)count * dimensions > 100_000_000)
            throw new InvalidDataException("无效的 word2vec 头部，应为：词数 维数；向量总元素上限 1 亿。");
        return (count, dimensions);
    }
    private static WordVectors ReadBinary(Stream stream, CancellationToken cancellation, IProgress<VectorProgress>? progress)
    {
        string countText = Word(stream); string dimensionText = Word(stream); var (count, dimensions) = Header(countText + " " + dimensionText);
        if (stream.Length - stream.Position < (long)count * (dimensions * 4L + 2)) throw new InvalidDataException("模型被截断或头部词数错误。");
        cancellation.ThrowIfCancellationRequested(); var map = new Dictionary<string, float[]>(Math.Min(count, 100_000), StringComparer.Ordinal); byte[] bytes = new byte[dimensions * sizeof(float)];
        for (int n = 0; n < count; n++)
        {
            cancellation.ThrowIfCancellationRequested(); string word = Word(stream); stream.ReadExactly(bytes); var vector = new float[dimensions];
            for (int d = 0; d < dimensions; d++) vector[d] = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(d * 4, 4)));
            Normalize(vector); if (!map.TryAdd(word, vector)) throw new InvalidDataException($"模型含重复词：{word}");
            if (n % 512 == 0 || n == count - 1) progress?.Report(new(n + 1, count));
        }
        return new(map, dimensions);
    }
    private static string Word(Stream stream)
    {
        var bytes = new List<byte>(); int b;
        do { b = stream.ReadByte(); if (b < 0) throw new EndOfStreamException("模型提前结束。"); } while (b is 10 or 13 or 32 or 9);
        while (b is not (10 or 13 or 32 or 9))
        {
            bytes.Add((byte)b); if (bytes.Count > 4096) throw new InvalidDataException("词条过长，文件可能不是 word2vec 格式。");
            b = stream.ReadByte(); if (b < 0) throw new EndOfStreamException("词条后缺少向量。");
        }
        string value = new UTF8Encoding(false, true).GetString(bytes.ToArray()); if (value.Length == 0) throw new InvalidDataException("空词条。"); return value;
    }
    private static WordVectors ReadText(Stream stream, CancellationToken cancellation, IProgress<VectorProgress>? progress)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false, true)); var (count, dimensions) = Header(ReadBoundedLine(reader, 128, cancellation) ?? "");
        cancellation.ThrowIfCancellationRequested(); var map = new Dictionary<string, float[]>(Math.Min(count, 100_000), StringComparer.Ordinal);
        for (int n = 0; n < count; n++)
        {
            cancellation.ThrowIfCancellationRequested(); string line = ReadBoundedLine(reader, 4096 + dimensions * 129, cancellation) ?? throw new InvalidDataException("模型被截断。");
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries); if (parts.Length != dimensions + 1) throw new InvalidDataException($"第 {n + 2} 行向量维数错误。");
            if (parts[0].Length > 4096) throw new InvalidDataException($"第 {n + 2} 行词条过长。");
            var vector = new float[dimensions];
            for (int d = 0; d < dimensions; d++)
                if (!float.TryParse(parts[d + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out vector[d])) throw new InvalidDataException($"第 {n + 2} 行不是浮点数。");
            Normalize(vector); if (!map.TryAdd(parts[0], vector)) throw new InvalidDataException($"模型含重复词：{parts[0]}");
            if (n % 512 == 0 || n == count - 1) progress?.Report(new(n + 1, count));
        }
        return new(map, dimensions);
    }
    private static string? ReadBoundedLine(TextReader reader, int maximumCharacters, CancellationToken cancellation)
    {
        var line = new StringBuilder(); int value;
        while ((value = reader.Read()) >= 0)
        {
            if (line.Length % 1024 == 0) cancellation.ThrowIfCancellationRequested();
            if (value == '\n') return line.ToString();
            if (value == '\r') { if (reader.Peek() == '\n') reader.Read(); return line.ToString(); }
            if (line.Length >= maximumCharacters) throw new InvalidDataException("模型中的单行过长，文件可能不是 word2vec 格式。");
            line.Append((char)value);
        }
        return line.Length > 0 ? line.ToString() : null;
    }
    private static void Normalize(float[] vector)
    {
        double sum = 0; foreach (float number in vector) { if (!float.IsFinite(number)) throw new InvalidDataException("模型包含 NaN 或无穷值。"); sum += (double)number * number; }
        if (sum <= 0) throw new InvalidDataException("模型包含全零向量。"); double norm = Math.Sqrt(sum);
        for (int d = 0; d < vector.Length; d++) vector[d] = (float)(vector[d] / norm);
    }
    public double? Similarity(string first, string second) => _vectors.TryGetValue(first, out var a) && _vectors.TryGetValue(second, out var b) ? Dot(a, b) : null;
    private static double Dot(float[] a, float[] b) { double sum = 0; for (int d = 0; d < a.Length; d++) sum += (double)a[d] * b[d]; return Math.Clamp(sum, -1, 1); }
    public IReadOnlyList<SimilarWord> Nearest(IEnumerable<string> context, int count = 10, int? wordLength = null, CancellationToken cancellation = default)
    {
        if (count is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(count));
        var reference = context.Distinct(StringComparer.Ordinal).Where(_vectors.ContainsKey).ToArray(); if (reference.Length == 0) return [];
        var target = new float[Dimensions]; foreach (string word in reference) for (int d = 0; d < Dimensions; d++) target[d] += _vectors[word][d];
        double norm = Math.Sqrt(target.Sum(n => (double)n * n)); if (norm <= 0) return []; for (int d = 0; d < Dimensions; d++) target[d] = (float)(target[d] / norm);
        var excluded = reference.ToHashSet(StringComparer.Ordinal); var heap = new PriorityQueue<SimilarWord, double>(); int index = 0;
        foreach (var (word, vector) in _vectors)
        {
            if (index++ % 256 == 0) cancellation.ThrowIfCancellationRequested();
            if (excluded.Contains(word) || wordLength.HasValue && word.Length != wordLength.Value) continue;
            double similarity = Dot(target, vector); heap.Enqueue(new(word, similarity), similarity); if (heap.Count > count) heap.Dequeue();
        }
        return heap.UnorderedItems.Select(x => x.Element).OrderByDescending(x => x.Similarity).ThenBy(x => x.Word, StringComparer.Ordinal).ToArray();
    }
}
