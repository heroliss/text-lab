using System.Globalization;
using System.Text;
using System.Text.Json;
using Liss.TextLab.Core;

var results = new List<object>(); int passed = 0, failed = 0; var segmenter = new SegmentService();
string temporary = Path.Combine(Path.GetTempPath(), "Liss.TextLab.Checks", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary); Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
void Check(string name, Action action)
{
    try { action(); passed++; results.Add(new { name, passed = true }); Console.WriteLine("PASS " + name); }
    catch (Exception e) { failed++; results.Add(new { name, passed = false, error = e.ToString() }); Console.WriteLine("FAIL " + name + " " + e.Message); }
}
void Assert(bool condition, string message = "Assertion failed") { if (!condition) throw new InvalidOperationException(message); }
void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
string Write(string name, string text, Encoding? encoding = null) { string path = Path.Combine(temporary, name); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, text, encoding ?? new UTF8Encoding(false)); return path; }
string vectorPath = Write("vectors.txt", "5 3\n算法 1 0 0\n计算 0.9 0.1 0\n游戏 0 1 0\n开发 0.1 0.9 0\n天气 0 0 1\n");
var vectors = WordVectors.Load(vectorPath);
try
{
    Check("accurate: known Chinese sentence", () => { var r = segmenter.Segment("我来到北京清华大学"); Assert(r.Tokens.Select(t => t.Word).SequenceEqual(["我", "来到", "北京", "清华大学"])); });
    Check("token offsets round-trip", () => { const string text = "中文、English 123\n游戏开发😀"; foreach (var t in segmenter.Segment(text).Tokens) Assert(text[t.Start..t.End] == t.Word); });
    Check("search mode expands long words", () => { var r = segmenter.Segment("清华大学", SegmentMode.Search); Assert(r.Tokens.Any(t => t.Word == "清华") && r.Tokens.Any(t => t.Word == "大学")); });
    Check("full mode retains overlapping offsets", () => { const string text = "南京市长江大桥"; var r = segmenter.Segment(text, SegmentMode.Full); Assert(r.Tokens.Count > 3); foreach (var t in r.Tokens) Assert(text[t.Start..t.End] == t.Word); });
    Check("frequency excludes punctuation", () => { var r = segmenter.Segment("算法，算法。算法！"); Assert(r.Frequencies.Single().Count == 3 && r.Frequencies.Single().Word == "算法"); });
    Check("empty segmentation", () => Assert(segmenter.Segment("").Tokens.Count == 0));
    Check("whitespace segmentation", () => Assert(segmenter.Segment(" \n\t").Tokens.Count == 0));
    Check("custom word applies", () => { segmenter.AddWord("星际文本实验室"); Assert(segmenter.Segment("打开星际文本实验室").Tokens.Any(t => t.Word == "星际文本实验室")); });
    Check("invalid custom word", () => Throws<ArgumentException>(() => segmenter.AddWord("two words")));
    Check("input size guard", () => Throws<ArgumentException>(() => segmenter.Segment(new string('a', 2_000_001))));
    Check("segmentation cancellation", () => { using var c = new CancellationTokenSource(); c.Cancel(); Throws<OperationCanceledException>(() => segmenter.Segment("文本", cancellation: c.Token)); });
    Check("chunk boundaries preserve Unicode", () => { string text = new string('中', 4095) + "😀\n算法"; foreach (var t in segmenter.Segment(text).Tokens) Assert(text[t.Start..t.End] == t.Word); });
    Check("fixed phrase finds repeated occurrences", () => { var r = new Proofreader(segmenter).Analyze("再接再励，再接再励。迫不急待。"); Assert(r.Issues.Count == 3); Assert(r.Issues[0].Suggestion == "再接再厉"); });
    Check("correct phrases are not flagged", () => Assert(new Proofreader(segmenter).Analyze("再接再厉，迫不及待，一如既往。").Issues.Count == 0));
    Check("apply selected issue", () => { var issue = new Proofreader(segmenter).Analyze("再接再励").Issues.Single(); Assert(Proofreader.Apply("再接再励", issue) == "再接再厉"); });
    Check("reject stale correction", () => { var issue = new Proofreader(segmenter).Analyze("再接再励").Issues.Single(); Throws<InvalidOperationException>(() => Proofreader.Apply("文本改了", issue)); });
    Check("proofreading input guard", () => Throws<ArgumentException>(() => new Proofreader(segmenter).Analyze(new string('a', 200001))));
    Check("context covers both sides and excludes self", () => { var r = new Proofreader(segmenter).Analyze("算法 计算 天气", vectors, 2); var center = r.References.Single(x => x.Word == "计算"); Assert(center.ContextWords == 2); Assert(center.AverageSimilarity > 0.49 && center.AverageSimilarity < 0.51); });
    Check("unknown vectors are skipped explicitly", () => Assert(new Proofreader(segmenter).Analyze("未知单词", vectors).References.Count == 0));
    Check("text word2vec metadata", () => Assert(vectors.Count == 5 && vectors.Dimensions == 3));
    Check("normalization preserves cosine", () => Assert(vectors.Similarity("算法", "计算") is > 0.99 and < 1));
    Check("unknown similarity is null", () => Assert(vectors.Similarity("算法", "不存在") is null));
    Check("nearest excludes query itself", () => { var r = vectors.Nearest(["算法"], 3); Assert(r.First().Word == "计算" && r.All(x => x.Word != "算法")); });
    Check("nearest length filter", () => Assert(vectors.Nearest(["算法"], 3, 3).Count == 0));
    Check("unknown query returns empty", () => Assert(vectors.Nearest(["missing"]).Count == 0));
    Check("invalid nearest count", () => Throws<ArgumentOutOfRangeException>(() => vectors.Nearest(["算法"], 0)));
    Check("vector import cancellation", () => { using var c = new CancellationTokenSource(); c.Cancel(); Throws<OperationCanceledException>(() => WordVectors.Load(vectorPath, c.Token)); });
    Check("truncated text vector rejected", () => Throws<InvalidDataException>(() => WordVectors.Load(Write("truncated.vec", "2 3\n一个 1 0 0\n"))));
    Check("NaN rejected", () => Throws<InvalidDataException>(() => WordVectors.Load(Write("nan.vec", "1 1\n坏 NaN\n"))));
    Check("zero vector rejected", () => Throws<InvalidDataException>(() => WordVectors.Load(Write("zero.vec", "1 2\n坏 0 0\n"))));
    Check("duplicate words rejected", () => Throws<InvalidDataException>(() => WordVectors.Load(Write("duplicate.vec", "2 1\n坏 1\n坏 1\n"))));
    Check("model allocation guard", () => Throws<InvalidDataException>(() => WordVectors.Load(Write("huge.vec", "2000000 4096\n"))));
    Check("binary little endian word2vec", () => { string path = Path.Combine(temporary, "small.bin"); using (var f = File.Create(path)) { f.Write(Encoding.UTF8.GetBytes("2 2\n中文 ")); using var w = new BinaryWriter(f, Encoding.UTF8, true); w.Write(3f); w.Write(4f); f.Write(Encoding.UTF8.GetBytes("\n相近 ")); w.Write(3f); w.Write(4f); } var m = WordVectors.Load(path); Assert(m.Count == 2 && m.Similarity("中文", "相近") > 0.999); });
    Check("truncated binary rejected", () => { string path = Path.Combine(temporary, "bad.bin"); File.WriteAllBytes(path, Encoding.UTF8.GetBytes("20 300\n")); Throws<InvalidDataException>(() => WordVectors.Load(path)); });
    string root = Path.Combine(temporary, "search"); Directory.CreateDirectory(root);
    File.WriteAllText(Path.Combine(root, "utf8.TXT"), "首行\n算法 algorithm ALGORITHM\n算法再次出现", new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(root, "gb.txt"), "旧编码\n算法", Encoding.GetEncoding(54936));
    File.WriteAllText(Path.Combine(root, "utf16.txt"), "UTF16\n算法", Encoding.Unicode);
    File.WriteAllText(Path.Combine(root, "ascii.txt"), "algorithm", new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(root, "ignored.cs"), "算法", new UTF8Encoding(false)); File.WriteAllBytes(Path.Combine(root, "binary.txt"), [0, 1, 0, 2, 0]);
    Check("UTF-8 decoding", () => Assert(TextFiles.Read(Path.Combine(root, "utf8.TXT")).Encoding == "UTF-8"));
    Check("GB18030 decoding", () => { var r = TextFiles.Read(Path.Combine(root, "gb.txt")); Assert(r.Encoding == "GB18030" && r.Text.Contains("算法")); });
    Check("UTF-16 BOM decoding", () => Assert(TextFiles.Read(Path.Combine(root, "utf16.txt")).Text.Contains("算法")));
    Check("BOM wins explicit encoding and label remains true", () => Assert(TextFiles.Read(Path.Combine(root, "utf16.txt"), TextEncodingChoice.Utf8).Encoding == "UTF-16 LE"));
    Check("binary null byte detection", () => Throws<InvalidDataException>(() => TextFiles.Read(Path.Combine(root, "binary.txt"))));
    Check("single decoding pass prevents duplicates", () => { var r = FileSearcher.Search(new(root, "algorithm", ".txt")); Assert(r.Hits.Count == 2 && r.Hits.Sum(h => h.Occurrences) == 3); });
    Check("multi encoding file results", () => { var r = FileSearcher.Search(new(root, "算法", ".txt")); Assert(r.Hits.Count == 4 && r.MatchedFiles == 3 && r.Skipped == 1); Assert(r.Hits.Any(h => h.Encoding == "GB18030")); });
    Check("case-sensitive search", () => { var r = FileSearcher.Search(new(root, "ALGORITHM", ".txt", false)); Assert(r.Hits.Count == 1 && r.Hits.Single().Occurrences == 1); });
    Check("extension matching is case-insensitive", () => Assert(FileSearcher.Search(new(root, "首行", ".txt")).Hits.Count == 1));
    Check("empty keyword rejected", () => Throws<ArgumentException>(() => FileSearcher.Search(new(root, " "))));
    Check("multiline keyword rejected", () => Throws<ArgumentException>(() => FileSearcher.Search(new(root, "a\nb"))));
    Check("missing directory rejected", () => Throws<DirectoryNotFoundException>(() => FileSearcher.Search(new(Path.Combine(root, "missing"), "a"))));
    Check("result line limit is explicit", () => { var r = FileSearcher.Search(new(root, "算法", ".txt", MaximumHits: 1)); Assert(r.Hits.Count == 1 && r.LimitReached); });
    Check("file size limit reports skip", () => { var r = FileSearcher.Search(new(root, "算法", ".txt", MaximumFileBytes: 1)); Assert(r.Hits.Count == 0 && r.Skipped >= 4); });
    Check("pre-cancel returns structured partial results", () => { using var c = new CancellationTokenSource(); c.Cancel(); var r = FileSearcher.Search(new(root, "算法"), cancellation: c.Token); Assert(r.Cancelled && r.Hits.Count == 0); });
    Check("cancel preserves earlier results", () => { using var c = new CancellationTokenSource(); var progress = new InlineProgress<FileProgress>(p => { if (p.Scanned >= 2) c.Cancel(); }); var r = FileSearcher.Search(new(root, "algorithm", ".txt"), progress, c.Token); Assert(r.Cancelled && r.Hits.Count == 1); });
    Check("long line preview stays bounded", () => { string path = Write("long.txt", new string('a', 1_000_000) + "关键字" + new string('b', 1000)); string folder = Path.Combine(temporary, "long-folder"); Directory.CreateDirectory(folder); File.Copy(path, Path.Combine(folder, "long.txt")); var r = FileSearcher.Search(new(folder, "关键字", ".txt")); Assert(r.Hits.Single().Snippet.Length <= 242 && r.Hits.Single().Line == 1); });
    Check("UTF-8 multibyte across detection buffer", () => { string text = new string('a', 8191) + "中文"; Assert(TextFiles.Read(Write("boundary.txt", text)).Text == text); });
    Check("KMP crosses character read buffer", () => { string folder = Path.Combine(temporary, "boundary-folder"); Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "data.txt"), new string('a', 8191) + "关键字\r\n关键字\r第三行关键字\n第四行", new UTF8Encoding(false)); var r = FileSearcher.Search(new(folder, "关键字", ".txt")); Assert(r.Hits.Select(h => h.Line).SequenceEqual([1, 2, 3])); });
    Check("KMP repeated prefix uses nonoverlapping matches", () => { string folder = Path.Combine(temporary, "prefix-folder"); Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "data.txt"), "abababab\nAAAaaaAA\n中文A中文a", new UTF8Encoding(false)); Assert(FileSearcher.Search(new(folder, "abab", ".txt")).Hits.Single().Occurrences == 2); Assert(FileSearcher.Search(new(folder, "aaa", ".txt")).Hits.Single().Occurrences == 2); });
    Check("KMP matches randomized reference", () => { var random = new Random(12345); string[] lines = Enumerable.Range(0, 60).Select(_ => new string(Enumerable.Range(0, 300).Select(_ => "aABb中"[random.Next(5)]).ToArray())).ToArray(); string folder = Path.Combine(temporary, "random-folder"); Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "data.txt"), string.Join("\r\n", lines), new UTF8Encoding(false)); var r = FileSearcher.Search(new(folder, "aB", ".txt")); for (int n = 0; n < lines.Length; n++) { int expected = 0, index = 0; while ((index = lines[n].IndexOf("aB", index, StringComparison.OrdinalIgnoreCase)) >= 0) { expected++; index += 2; } Assert(r.Hits.Single(h => h.Line == n + 1).Occurrences == expected); } });
    Check("larger model cancellation at import checkpoint", () => { var builder = new StringBuilder("20000 32\n"); for (int n = 0; n < 20000; n++) builder.Append("word" + n.ToString(CultureInfo.InvariantCulture) + " ").AppendJoin(' ', Enumerable.Range(0, 32).Select(d => d == n % 32 ? "1" : "0")).Append('\n'); string path = Write("larger.vec", builder.ToString()); using var c = new CancellationTokenSource(); var progress = new InlineProgress<VectorProgress>(p => { if (p.Loaded >= 513) c.Cancel(); }); Throws<OperationCanceledException>(() => WordVectors.Load(path, c.Token, progress)); });
    string batchSource = Path.Combine(temporary, "batch-in"), batchOutput = Path.Combine(temporary, "batch-out"); Directory.CreateDirectory(batchSource); Directory.CreateDirectory(Path.Combine(batchSource, "nested"));
    File.WriteAllText(Path.Combine(batchSource, "one.txt"), "算法与算法"); File.WriteAllText(Path.Combine(batchSource, "nested", "two.md"), "北京清华大学");
    Check("batch retains relative structure and UTF-8 output", () => { var r = BatchSegmenter.Run(segmenter, batchSource, batchOutput); Assert(r.Count == 2 && File.Exists(Path.Combine(batchOutput, "nested", "two.md.segmented.txt"))); Assert(File.ReadAllText(Path.Combine(batchOutput, "one.txt.segmented.txt")).Contains("算法 / 与 / 算法")); });
    Check("batch does not change source", () => Assert(File.ReadAllText(Path.Combine(batchSource, "one.txt")) == "算法与算法"));
    Check("batch rejects overlapping directories", () => Throws<ArgumentException>(() => BatchSegmenter.Run(segmenter, batchSource, Path.Combine(batchSource, "out"))));
    Check("batch cancellation leaves no temp files", () => { using var c = new CancellationTokenSource(); c.Cancel(); Throws<OperationCanceledException>(() => BatchSegmenter.Run(segmenter, batchSource, batchOutput, cancellation: c.Token)); Assert(Directory.GetFiles(batchOutput, "*.tmp", SearchOption.AllDirectories).Length == 0); });
}
finally { Directory.Delete(temporary, true); }
string? report = args.Length > 0 ? args[0] : null; var summary = new { passed, failed, framework = Environment.Version.ToString(), cases = results };
if (report is not null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!); File.WriteAllText(report, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true })); }
Console.WriteLine($"{passed} passed / {failed} failed"); return failed == 0 ? 0 : 1;

sealed class InlineProgress<T>(Action<T> handler) : IProgress<T> { public void Report(T value) => handler(value); }
