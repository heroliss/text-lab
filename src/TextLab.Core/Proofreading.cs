namespace Liss.TextLab.Core;

public sealed record CorrectionRule(string Wrong, string Right, string Explanation);
public sealed record CorrectionIssue(int Start, int Length, string Original, string Suggestion, string Kind, string Reason);
public sealed record SemanticReference(string Word, int Start, double AverageSimilarity, int ContextWords);
public sealed record ProofreadResult(IReadOnlyList<CorrectionIssue> Issues, IReadOnlyList<SemanticReference> References);

public sealed class Proofreader(SegmentService segmenter)
{
    // Independently authored review rules; this list is not a trained language model.
    public static IReadOnlyList<CorrectionRule> Rules { get; } = new CorrectionRule[]
    {
        new("再接再励", "再接再厉", "固定成语；厉指磨砺。"), new("迫不急待", "迫不及待", "固定成语；及表示来得及。"),
        new("一如继往", "一如既往", "固定成语；既往指过去。"), new("名符其实", "名副其实", "固定用语；副表示相称。"),
        new("默守成规", "墨守成规", "固定成语；墨守源自墨子守城。"), new("按步就班", "按部就班", "固定成语；部指门类和次序。"),
        new("相辅相承", "相辅相成", "固定成语；相互补充而成就。"), new("穿流不息", "川流不息", "固定成语；川流比喻连续不断。"),
        new("谈笑风声", "谈笑风生", "固定成语；风生形容兴致。"), new("走投无路", "走投无路", ""),
        new("受益非浅", "受益匪浅", "固定用语；匪表示不。"), new("世外桃园", "世外桃源", "典故用语；桃源指理想之地。"),
        new("美仑美奂", "美轮美奂", "固定成语；轮形容建筑高大。"), new("迫在眉捷", "迫在眉睫", "固定成语；睫指眼睫毛。"),
        new("出类拔粹", "出类拔萃", "固定成语；萃指聚集的人或物。"), new("不径而走", "不胫而走", "固定成语；胫指小腿。")
    }.Where(r => r.Wrong != r.Right).ToArray();
    public ProofreadResult Analyze(string text, WordVectors? model = null, int window = 3, CancellationToken cancellation = default)
    {
        if (text.Length > 200_000) throw new ArgumentException("校对文本最多 20 万字符。");
        if (window is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(window));
        var issues = new List<CorrectionIssue>();
        foreach (var rule in Rules)
        {
            cancellation.ThrowIfCancellationRequested(); int index = 0;
            while ((index = text.IndexOf(rule.Wrong, index, StringComparison.Ordinal)) >= 0)
            { issues.Add(new(index, rule.Wrong.Length, rule.Wrong, rule.Right, "固定用语", rule.Explanation)); index += rule.Wrong.Length; }
        }
        var references = new List<SemanticReference>();
        if (model is not null)
        {
            var tokens = segmenter.Segment(text, cancellation: cancellation).Tokens.Where(t => t.Word.Any(char.IsLetterOrDigit)).ToArray();
            for (int i = 0; i < tokens.Length; i++)
            {
                cancellation.ThrowIfCancellationRequested(); if (!model.Contains(tokens[i].Word)) continue;
                double sum = 0; int count = 0;
                for (int j = Math.Max(0, i - window); j <= Math.Min(tokens.Length - 1, i + window); j++)
                {
                    if (i == j) continue; var score = model.Similarity(tokens[i].Word, tokens[j].Word); if (score.HasValue) { sum += score.Value; count++; }
                }
                if (count > 0) references.Add(new(tokens[i].Word, tokens[i].Start, sum / count, count));
            }
        }
        return new(issues.OrderBy(i => i.Start).ToArray(), references.OrderBy(r => r.AverageSimilarity).Take(30).ToArray());
    }
    public static string Apply(string text, CorrectionIssue issue)
    {
        if (issue.Start < 0 || issue.Length <= 0 || issue.Start + issue.Length > text.Length || text.Substring(issue.Start, issue.Length) != issue.Original)
            throw new InvalidOperationException("原文已改变，请重新校对后应用建议。");
        return text.Remove(issue.Start, issue.Length).Insert(issue.Start, issue.Suggestion);
    }
}
