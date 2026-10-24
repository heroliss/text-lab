using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Liss.TextLab.Core;

namespace Liss.TextLab.App;

internal sealed class MainForm : Form
{
    private const string SampleText = "算法让复杂的问题变得可以理解。中文分词通过词典构建有向无环图，再用动态规划寻找合理的切分路径。\n\n在游戏开发中，算法与数据结构帮助我们组织世界；在文本处理中，算法帮助我们发现文字背后的结构。机器学习、游戏开发和自然语言处理，也可以在同一个实验室里相遇。";
    private const string ProofText = "一次小小的尝试，也能带来变化。\n\n我们迫不急待地开始新的实验，遇到困难仍然再接再励。走出熟悉的边界，探索算法的可能性，让设计与代码相辅相承。\n\n保持一如继往的好奇心，让每一次学习都受益非浅。";
    private readonly string[] _args;
    private readonly SegmentService _segmenter = new();
    private WordVectors? _model;
    private CancellationTokenSource? _job;
    private readonly Panel _workspace = new() { Dock = DockStyle.Fill, Padding = new Padding(22, 16, 12, 8), BackColor = Theme.Background };
    private readonly Label _status = Theme.Label("就绪 · 所有文本处理均在本机完成", 9, Theme.Muted);
    private readonly Button _cancel = Theme.Button("取消任务", false, 100);
    private readonly List<Button> _navigation = [];
    private readonly Label _metricA = Theme.Label("—", 19, Theme.Accent, FontStyle.Bold), _metricB = Theme.Label("—", 19, Theme.Blue, FontStyle.Bold), _metricC = Theme.Label("—", 19, Theme.Purple, FontStyle.Bold);
    private readonly RichTextBox _segmentInput = Theme.Editor(SampleText), _segmentOutput = Theme.Editor(readOnly: true), _proofInput = Theme.Editor(ProofText), _proofDetails = Theme.Editor(readOnly: true), _searchPreview = Theme.Editor(readOnly: true);
    private readonly ComboBox _segmentMode = Theme.Combo("精确模式", "搜索模式", "全模式"), _encoding = Theme.Combo("自动识别编码", "UTF-8", "GB18030", "UTF-16 LE");
    private readonly TextBox _customWord = Theme.Input(), _vectorQuery = Theme.Input("算法 游戏开发"), _path = Theme.Input(), _keyword = Theme.Input("算法"), _extensions = Theme.Input(".txt,.md,.cs,.cpp,.h,.json,.csv,.log");
    private readonly Label _modelLabel = Theme.Label("未加载词向量模型", 9.5f, Theme.Muted), _segmentNote = Theme.Label("Jieba · DAG + 动态规划 + HMM", 9, Theme.Muted);
    private readonly FlowLayoutPanel _chips = new() { Dock = DockStyle.Fill, BackColor = Theme.Surface, AutoScroll = true, Padding = new Padding(0, 4, 20, 4) };
    private readonly FrequencyBars _bars = new();
    private readonly DataGridView _frequencyGrid = Theme.Grid(("word", "词语", 55), ("count", "次数", 20), ("start", "位置", 25));
    private readonly DataGridView _issuesGrid = Theme.Grid(("original", "原文", 22), ("suggestion", "建议", 22), ("position", "位置", 14), ("type", "类型", 22));
    private readonly DataGridView _vectorGrid = Theme.Grid(("word", "参考词", 60), ("similarity", "余弦相似度", 40));
    private readonly DataGridView _hitsGrid = Theme.Grid(("file", "文件", 31), ("line", "行", 8), ("snippet", "匹配内容", 47), ("encoding", "编码", 14));
    private readonly NumericUpDown _maxMb = new() { Minimum = 1, Maximum = 2048, Value = 64, Width = 70, BackColor = Theme.Raised, ForeColor = Theme.Text, Font = Theme.Font(), Margin = new Padding(0, 6, 8, 6) };
    private readonly NumericUpDown _maxHits = new() { Minimum = 10, Maximum = 1_000_000, Value = 10000, Increment = 1000, Width = 95, BackColor = Theme.Raised, ForeColor = Theme.Text, Font = Theme.Font(), Margin = new Padding(0, 6, 8, 6) };
    private readonly CheckBox _ignoreCase = new() { Text = "忽略大小写", Checked = true, AutoSize = true, ForeColor = Theme.Text, Margin = new Padding(4, 10, 12, 4) };
    private readonly Panel[] _pages;
    private SegmentResult? _segments;
    private ProofreadResult? _proof;
    private SearchSummary? _search;
    private string _searchRoot = "";
    private int _page;
    private bool _loadingProof;

    public MainForm(string[] args)
    {
        _args = args; Text = "LISS / Text Lab 1.0 — 文字处理实验室"; ClientSize = new Size(1380, 920); MinimumSize = new Size(1120, 760); BackColor = Theme.Background; ForeColor = Theme.Text; Font = Theme.Font(); StartPosition = FormStartPosition.CenterScreen;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 182)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        var brand = Theme.Label("LISS\nTEXT LAB", 16, Theme.Accent, FontStyle.Bold); brand.Padding = new Padding(22, 4, 0, 0); brand.BackColor = Theme.Surface; root.Controls.Add(brand, 0, 0);
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(24, 10, 18, 10) }; header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        header.Controls.Add(Theme.Label("文字处理实验室   /   LOCAL TEXT WORKBENCH", 15, Theme.Text, FontStyle.Bold), 0, 0); _cancel.Enabled = false; _cancel.Click += (_, _) => { _job?.Cancel(); _cancel.Enabled = false; SetStatus("正在取消，等待当前安全检查点…"); }; header.Controls.Add(_cancel, 1, 0); root.Controls.Add(header, 1, 0);
        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Surface, Padding = new Padding(14, 22, 10, 10) };
        foreach (var (title, id) in new[] { ("01   分词与词频", 0), ("02   校对与词向量", 1), ("03   文件内容检索", 2) })
        { var button = Theme.Button(title, false, 154); button.Height = 52; button.TextAlign = ContentAlignment.MiddleLeft; button.Padding = new Padding(8, 0, 0, 0); button.Click += (_, _) => Navigate(id); nav.Controls.Add(button); _navigation.Add(button); }
        var note = Theme.Label("离线优先\n\nUTF-8 / GB18030\n\n词典 · 向量 · 文件", 9, Theme.Muted); note.Dock = DockStyle.None; note.Size = new Size(150, 180); note.Margin = new Padding(8, 35, 0, 0); note.TextAlign = ContentAlignment.TopLeft; nav.Controls.Add(note);
        root.Controls.Add(nav, 0, 1); root.SetRowSpan(nav, 2); root.Controls.Add(_workspace, 1, 1); _status.Padding = new Padding(24, 0, 0, 0); root.Controls.Add(_status, 1, 2); Controls.Add(root);
        _pages = [SegmentPage(), ProofPage(), SearchPage()]; Navigate(0);
        _proofDetails.Font = Theme.Font(10.5f); _searchPreview.Font = Theme.Font(10.5f);
        _segmentInput.TextChanged += (_, _) => SegmentChanged(); _segmentMode.SelectedIndexChanged += (_, _) => SegmentChanged();
        Shown += async (_, _) => await InitializeShowcase(); FormClosing += (_, _) => _job?.Cancel();
    }

    private static TableLayoutPanel Rows(params (SizeType Type, float Size)[] styles)
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = styles.Length, BackColor = Theme.Background, Margin = Padding.Empty };
        foreach (var (type, size) in styles) table.RowStyles.Add(new RowStyle(type, size)); return table;
    }
    private static TableLayoutPanel Columns(float leftPercent = 60)
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty }; table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftPercent)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 - leftPercent)); return table;
    }
    private Control Intro(string eyebrow, string title, string detail)
    {
        var table = Rows((SizeType.Absolute, 22), (SizeType.Absolute, 36), (SizeType.Percent, 100)); table.Controls.Add(Theme.Label(eyebrow, 8.5f, Theme.Accent, FontStyle.Bold), 0, 0);
        table.Controls.Add(Theme.Label(title, 21, Theme.Text, FontStyle.Bold), 0, 1); table.Controls.Add(Theme.Label(detail, 9.5f, Theme.Muted), 0, 2); return table;
    }
    private Control Metrics(params (string Caption, Label Value, string Detail)[] values)
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = values.Length, RowCount = 1, Margin = Padding.Empty };
        for (int i = 0; i < values.Length; i++)
        {
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / values.Length)); var card = Rows((SizeType.Absolute, 25), (SizeType.Absolute, 35), (SizeType.Percent, 100)); card.BackColor = Theme.Surface; card.Padding = new Padding(16, 5, 8, 4); card.Margin = new Padding(0, 0, 12, 12);
            card.Controls.Add(Theme.Label(values[i].Caption, 9, Theme.Muted), 0, 0); card.Controls.Add(values[i].Value, 0, 1); card.Controls.Add(Theme.Label(values[i].Detail, 8, Theme.Muted), 0, 2); table.Controls.Add(card, i, 0);
        }
        return table;
    }
    private Panel SegmentPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill }; var table = Rows((SizeType.Absolute, 92), (SizeType.Absolute, 96), (SizeType.Absolute, 54), (SizeType.Percent, 100));
        table.Controls.Add(Intro("TOKENIZE / FREQUENCY / POSITION", "让文字的结构清晰可见", "切分文本、观察词频，并保留每个词在原文中的位置。"), 0, 0);
        table.Controls.Add(Metrics(("词元数量", _metricA, "当前模式的切分结果"), ("不同词语", _metricB, "排除纯标点与空白"), ("文本长度", _metricC, "UTF-16 字符位置")), 0, 1);
        var toolbar = Theme.Toolbar(); var run = Theme.Button("开始分词", true, 112); run.Click += async (_, _) => await Segment(); toolbar.Controls.Add(run); toolbar.Controls.Add(_segmentMode);
        var import = Theme.Button("打开文本", false, 102); import.Click += async (_, _) => await OpenText(_segmentInput); toolbar.Controls.Add(import);
        var batch = Theme.Button("批量处理目录", false, 130); batch.Click += async (_, _) => await Batch(); toolbar.Controls.Add(batch);
        var export = Theme.Button("导出结果", false, 102); export.Click += (_, _) => SaveText(_segmentOutput.Text, "tokens.txt"); toolbar.Controls.Add(export); table.Controls.Add(toolbar, 0, 2);
        var split = Columns(55); var left = Rows((SizeType.Percent, 60), (SizeType.Percent, 40)); left.Controls.Add(Theme.Card("原始文本", _segmentInput), 0, 0);
        left.Controls.Add(Theme.Card("分词输出", _segmentOutput, "“ / ”分隔词元；导出为 UTF-8 纯文本。"), 0, 1); split.Controls.Add(left, 0, 0);
        var right = Rows((SizeType.Percent, 48), (SizeType.Percent, 52)); right.Controls.Add(Theme.Card("切分视图", _chips, "颜色用于区分相邻词元，不代表词性或置信度。"), 0, 0);
        var frequencies = Columns(52); frequencies.Controls.Add(_frequencyGrid, 0, 0); frequencies.Controls.Add(_bars, 1, 0);
        var custom = Rows((SizeType.Percent, 100), (SizeType.Absolute, 42), (SizeType.Absolute, 22)); custom.Controls.Add(frequencies, 0, 0); var addRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 }; addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); _customWord.PlaceholderText = "自定义词，如：星际文本实验室"; addRow.Controls.Add(_customWord, 0, 0); var add = Theme.Button("加入词典", false, 98);
        add.Click += async (_, _) => { try { _segmenter.AddWord(_customWord.Text.Trim()); SetStatus("自定义词已加入本次会话词典"); await Segment(); } catch (Exception ex) { ShowError(ex); } }; addRow.Controls.Add(add, 1, 0); custom.Controls.Add(addRow, 0, 1); custom.Controls.Add(_segmentNote, 0, 2);
        right.Controls.Add(Theme.Card("词频与词典", custom), 0, 1); split.Controls.Add(right, 1, 0); table.Controls.Add(split, 0, 3); panel.Controls.Add(table); return panel;
    }
    private Panel ProofPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill }; var table = Rows((SizeType.Absolute, 92), (SizeType.Absolute, 54), (SizeType.Percent, 100));
        table.Controls.Add(Intro("PROOFREAD / CONTEXT / WORD VECTORS", "检查固定用语，探索上下文", "建议由你确认；词向量是语义参考，不是通用语法检查器。"), 0, 0);
        var toolbar = Theme.Toolbar(); var run = Theme.Button("检查文本", true, 112); run.Click += async (_, _) => await Proof(); toolbar.Controls.Add(run);
        var apply = Theme.Button("应用所选建议", false, 135); apply.Click += async (_, _) => await ApplyIssue(); toolbar.Controls.Add(apply);
        var open = Theme.Button("打开文本", false, 102); open.Click += async (_, _) => await OpenText(_proofInput); toolbar.Controls.Add(open);
        var save = Theme.Button("保存文本", false, 102); save.Click += (_, _) => SaveText(_proofInput.Text, "reviewed.txt"); toolbar.Controls.Add(save); table.Controls.Add(toolbar, 0, 1);
        var split = Columns(59); var left = Rows((SizeType.Percent, 45), (SizeType.Percent, 55)); left.Controls.Add(Theme.Card("待校对文本", _proofInput), 0, 0); left.Controls.Add(Theme.Card("固定用语建议", _issuesGrid, $"内置 {Proofreader.Rules.Count} 条人工整理规则；仅匹配明确的固定用语。"), 0, 1); split.Controls.Add(left, 0, 0);
        var right = Rows((SizeType.Percent, 40), (SizeType.Percent, 60)); right.Controls.Add(Theme.Card("建议依据", _proofDetails), 0, 0);
        var model = Rows((SizeType.Absolute, 30), (SizeType.Absolute, 48), (SizeType.Absolute, 44), (SizeType.Percent, 100), (SizeType.Absolute, 44)); model.Controls.Add(_modelLabel, 0, 0);
        var buttons = Theme.Toolbar(); var load = Theme.Button("导入 word2vec", false, 150); load.Click += async (_, _) => await LoadModel(); buttons.Controls.Add(load); var unload = Theme.Button("卸载", false, 70); unload.Click += (_, _) => { _model = null; _modelLabel.Text = "未加载词向量模型"; _vectorGrid.Rows.Clear(); }; buttons.Controls.Add(unload); model.Controls.Add(buttons, 0, 1);
        var query = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 }; query.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); query.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102)); query.Controls.Add(_vectorQuery, 0, 0); var nearest = Theme.Button("语义近邻", false, 95); nearest.Click += async (_, _) => await Nearest(); query.Controls.Add(nearest, 1, 0); model.Controls.Add(query, 0, 2); model.Controls.Add(_vectorGrid, 0, 3);
        model.Controls.Add(Theme.Label("支持 UTF-8 文本 / 二进制模型；只导入你有权使用的文件。\n默认不附带预训练模型。", 8.5f, Theme.Muted), 0, 4); right.Controls.Add(Theme.Card("词向量工作台", model), 0, 1); split.Controls.Add(right, 1, 0); table.Controls.Add(split, 0, 2);
        _issuesGrid.SelectionChanged += (_, _) => SelectIssue(); _proofInput.TextChanged += (_, _) =>
        {
            if (_loadingProof) return; _proof = null; _issuesGrid.Rows.Clear(); _proofDetails.Text = "原文已改变。点击“检查文本”生成当前文本的建议。";
            _loadingProof = true; int start = _proofInput.SelectionStart, length = _proofInput.SelectionLength; _proofInput.SelectAll(); _proofInput.SelectionColor = Theme.Text; _proofInput.Select(start, length); _loadingProof = false;
        }; panel.Controls.Add(table); return panel;
    }
    private Panel SearchPage()
    {
        var panel = new Panel { Dock = DockStyle.Fill }; var table = Rows((SizeType.Absolute, 92), (SizeType.Absolute, 48), (SizeType.Absolute, 48), (SizeType.Percent, 62), (SizeType.Percent, 38));
        table.Controls.Add(Intro("FINDTEXT / STREAMING / ENCODING", "在文件中找到那一行", "递归检索本地文本，显示行号与上下文；搜索可以随时取消。"), 0, 0);
        var pathRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 }; pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70)); pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96)); pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        pathRow.Controls.Add(Theme.Label("目录", 10, Theme.Muted), 0, 0); pathRow.Controls.Add(_path, 1, 0); var browse = Theme.Button("选择", false, 85); browse.Click += (_, _) => { using var dialog = new FolderBrowserDialog(); if (dialog.ShowDialog(this) == DialogResult.OK) _path.Text = dialog.SelectedPath; }; pathRow.Controls.Add(browse, 2, 0); _encoding.Dock = DockStyle.Fill; pathRow.Controls.Add(_encoding, 3, 0); table.Controls.Add(pathRow, 0, 1);
        var searchRow = Theme.Toolbar(); _keyword.Width = 200; _keyword.Dock = DockStyle.None; _keyword.PlaceholderText = "关键词"; searchRow.Controls.Add(_keyword); var start = Theme.Button("开始检索", true, 110); start.Click += async (_, _) => await Search(); searchRow.Controls.Add(start); searchRow.Controls.Add(_ignoreCase);
        _extensions.Width = 200; _extensions.Dock = DockStyle.None; searchRow.Controls.Add(_extensions); var mb = Theme.Label("MiB/文件", 8, Theme.Muted); mb.Dock = DockStyle.None; mb.Size = new Size(70, 38); searchRow.Controls.Add(mb); searchRow.Controls.Add(_maxMb); var hits = Theme.Label("行上限", 8, Theme.Muted); hits.Dock = DockStyle.None; hits.Size = new Size(55, 38); searchRow.Controls.Add(hits); searchRow.Controls.Add(_maxHits); table.Controls.Add(searchRow, 0, 2);
        searchRow.WrapContents = true; searchRow.SizeChanged += (_, _) => { float height = searchRow.Width < 1080 ? 92 : 48; if (table.RowStyles[2].Height != height) table.RowStyles[2].Height = height; };
        table.Controls.Add(Theme.Card("匹配结果", _hitsGrid, "一个匹配行显示一次；双击可在预览中查看，原始文件保持只读。"), 0, 3);
        var preview = Rows((SizeType.Percent, 100), (SizeType.Absolute, 44)); preview.Controls.Add(_searchPreview, 0, 0); var actions = Theme.Toolbar(); var export = Theme.Button("导出结果 JSON", false, 150); export.Click += (_, _) => { if (_search is null) { SetStatus("请先完成一次搜索"); return; } SaveText(JsonSerializer.Serialize(_search, new JsonSerializerOptions { WriteIndented = true }), "search-results.json"); }; actions.Controls.Add(export); var openFolder = Theme.Button("打开所在目录", false, 135); openFolder.Click += (_, _) => { if (SelectedHit() is FileHit hit && Directory.Exists(Path.GetDirectoryName(hit.Path))) Process.Start(new ProcessStartInfo("explorer.exe", Path.GetDirectoryName(hit.Path)!) { UseShellExecute = true }); }; actions.Controls.Add(openFolder); preview.Controls.Add(actions, 0, 1); table.Controls.Add(Theme.Card("只读预览", preview), 0, 4);
        _hitsGrid.VirtualMode = true; _hitsGrid.CellValueNeeded += (_, e) =>
        {
            if (_search is null || e.RowIndex < 0 || e.RowIndex >= _search.Hits.Count) return; var hit = _search.Hits[e.RowIndex];
            e.Value = e.ColumnIndex switch { 0 => Path.GetRelativePath(_searchRoot, hit.Path), 1 => hit.Line, 2 => hit.Snippet, 3 => hit.Encoding, _ => null };
        };
        _hitsGrid.SelectionChanged += (_, _) => PreviewHit(); panel.Controls.Add(table); return panel;
    }
    private void Navigate(int page)
    {
        if (_job is not null) { SetStatus("请等待当前任务完成，或先取消任务。"); return; } _page = page; _workspace.Controls.Clear(); _workspace.Controls.Add(_pages[page]);
        for (int i = 0; i < _navigation.Count; i++) { _navigation[i].BackColor = page == i ? Theme.Raised : Theme.Surface; _navigation[i].ForeColor = page == i ? Theme.Accent : Theme.Muted; }
    }
    private async Task Job(string title, Func<CancellationToken, Task> action)
    {
        if (_job is not null) { SetStatus("已有任务在运行。可以先取消当前任务。"); return; }
        using var cancellation = new CancellationTokenSource(); _job = cancellation; _cancel.Enabled = true; var locks = LockInputs(); foreach (var button in _navigation) button.Enabled = false; SetStatus(title + "…");
        var watch = Stopwatch.StartNew();
        try { await action(cancellation.Token); SetStatus(_status.Text + $"  ·  {watch.Elapsed.TotalMilliseconds:N0} ms"); }
        catch (OperationCanceledException) { SetStatus("已取消 · 已完成的文件保留"); }
        catch (Exception e) { ShowError(e); }
        finally { _job = null; _cancel.Enabled = false; foreach (var restore in locks) restore(); foreach (var button in _navigation) button.Enabled = true; }
    }
    private List<Action> LockInputs()
    {
        var restore = new List<Action>();
        void Visit(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is TextBoxBase text) { bool value = text.ReadOnly; text.ReadOnly = true; restore.Add(() => text.ReadOnly = value); }
                else if (control is Button or ComboBox or NumericUpDown) { bool value = control.Enabled; control.Enabled = false; restore.Add(() => control.Enabled = value); }
                else Visit(control);
            }
        }
        Visit(_workspace); return restore;
    }
    private async Task Segment()
    {
        string text = _segmentInput.Text; var mode = (SegmentMode)_segmentMode.SelectedIndex;
        await Job("正在分词", async cancellation => { var result = await Task.Run(() => _segmenter.Segment(text, mode, cancellation: cancellation), cancellation); _segments = result; ShowSegments(result); SetStatus($"分词完成 · {result.Tokens.Count:N0} 个词元 · {result.Frequencies.Count:N0} 个不同词语"); });
    }
    private void SegmentChanged()
    {
        if (_segments is null) return; _segmentNote.Text = "输入或模式已改变；点击开始分词更新结果。"; if (_job is null) SetStatus("当前输入尚未分析 · 下方保留上次结果");
    }
    private void ShowSegments(SegmentResult result)
    {
        _metricA.Text = result.Tokens.Count.ToString("N0"); _metricB.Text = result.Frequencies.Count.ToString("N0"); _metricC.Text = result.Characters.ToString("N0"); _segmentOutput.Text = string.Join(" / ", result.Tokens.Select(t => t.Word));
        _chips.SuspendLayout(); foreach (Control control in _chips.Controls.Cast<Control>().ToArray()) control.Dispose(); _chips.Controls.Clear(); Color[] colors = [Theme.Accent, Theme.Blue, Theme.Purple, Theme.Amber]; int index = 0;
        foreach (var token in result.Tokens.Take(1000)) { var label = new Label { Text = token.Word, ForeColor = colors[index++ % colors.Length], BackColor = Theme.Raised, Font = Theme.Font(10), AutoSize = true, Padding = new Padding(8, 6, 8, 6), Margin = new Padding(0, 0, 6, 7) }; var tip = new ToolTip(); tip.SetToolTip(label, $"原文位置 [{token.Start}, {token.End})"); label.Disposed += (_, _) => tip.Dispose(); _chips.Controls.Add(label); }
        _chips.ResumeLayout(); _frequencyGrid.Rows.Clear(); foreach (var frequency in result.Frequencies.Take(500)) _frequencyGrid.Rows.Add(frequency.Word, frequency.Count, result.Tokens.First(t => t.Word == frequency.Word).Start);
        _bars.SetItems(result.Frequencies.Select(f => (f.Word, f.Count))); _segmentNote.Text = result.Tokens.Count > 1000 ? "视图显示前 1,000 个词元；导出包含全部结果。" : "Jieba · DAG + 动态规划 + HMM";
    }
    private async Task Proof()
    {
        string text = _proofInput.Text; var model = _model;
        await Job("正在校对", async cancellation => { var result = await Task.Run(() => new Proofreader(_segmenter).Analyze(text, model, cancellation: cancellation), cancellation); _proof = result; _issuesGrid.Rows.Clear(); foreach (var issue in result.Issues) { int row = _issuesGrid.Rows.Add(issue.Original, issue.Suggestion, issue.Start, issue.Kind); _issuesGrid.Rows[row].Tag = issue; } SelectIssue();
            _loadingProof = true; _proofInput.SelectAll(); _proofInput.SelectionColor = Theme.Text; foreach (var issue in result.Issues) { _proofInput.Select(issue.Start, issue.Length); _proofInput.SelectionColor = Theme.Amber; } _proofInput.Select(0, 0); _loadingProof = false;
            if (result.Issues.Count == 0) _proofDetails.Text = "未发现内置固定用语规则匹配。\n\n这不代表文本完全正确，请结合语境人工审阅。";
            if (model is not null)
            {
                _vectorGrid.Columns[0].HeaderText = "上下文词（位置）"; _vectorGrid.Columns[1].HeaderText = "平均相似度"; _vectorGrid.Rows.Clear();
                foreach (var reference in result.References) _vectorGrid.Rows.Add($"{reference.Word}（{reference.Start}）", reference.AverageSimilarity.ToString("F4"));
                _proofDetails.AppendText("\n\n右下表按上下文相似度从低到高排列。低分只提示语义差异，不能直接判断为错字。");
            }
            SetStatus($"校对完成 · {result.Issues.Count} 条固定用语建议" + (model is null ? " · 未启用词向量" : $" · {result.References.Count} 条上下文参考")); });
    }
    private void SelectIssue()
    {
        if (_issuesGrid.CurrentRow?.Tag is not CorrectionIssue issue) return; _proofDetails.Text = $"{issue.Original}  →  {issue.Suggestion}\n\n{issue.Reason}\n规则建议 · 位置 {issue.Start + 1}\n\n点击“应用所选建议”确认替换。"; _proofInput.Select(issue.Start, issue.Length);
    }
    private async Task ApplyIssue()
    {
        if (_issuesGrid.CurrentRow?.Tag is not CorrectionIssue issue) { SetStatus("请先选择一条建议"); return; }
        try { _proofInput.Text = Proofreader.Apply(_proofInput.Text, issue); await Proof(); } catch (Exception e) { ShowError(e); }
    }
    private async Task LoadModel()
    {
        using var dialog = new OpenFileDialog { Filter = "word2vec 模型|*.bin;*.vec;*.txt|所有文件|*.*" }; if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await Job("正在加载模型", async cancellation => { var progress = new Progress<VectorProgress>(p => SetStatus($"读取词向量 · {p.Loaded:N0} / {p.Total:N0}")); var model = await Task.Run(() => WordVectors.Load(dialog.FileName, cancellation, progress), cancellation); _model = model; _modelLabel.Text = $"{Path.GetFileName(dialog.FileName)} · {model.Count:N0} 词 × {model.Dimensions} 维"; SetStatus("模型已加载 · 原始文件保持只读"); });
    }
    private async Task Nearest()
    {
        if (_model is null) { SetStatus("请先导入你有权使用的 word2vec 模型"); return; } var model = _model; var query = _vectorQuery.Text.Split([' ', ',', '，'], StringSplitOptions.RemoveEmptyEntries);
        await Job("正在计算语义近邻", async cancellation => { var result = await Task.Run(() => model.Nearest(query, 15, cancellation: cancellation), cancellation); _vectorGrid.Columns[0].HeaderText = "参考词"; _vectorGrid.Columns[1].HeaderText = "余弦相似度"; _vectorGrid.Rows.Clear(); foreach (var item in result) _vectorGrid.Rows.Add(item.Word, item.Similarity.ToString("F4")); SetStatus(result.Count == 0 ? "词表中未找到有效查询词" : $"找到 {result.Count} 个语义近邻 · 仅供人工参考"); });
    }
    private async Task Search()
    {
        var options = new FileSearchOptions(_path.Text.Trim(), _keyword.Text, _extensions.Text, _ignoreCase.Checked, (TextEncodingChoice)_encoding.SelectedIndex, (long)_maxMb.Value * 1024 * 1024, (int)_maxHits.Value);
        _hitsGrid.RowCount = 0; _search = null; _searchPreview.Clear(); _searchRoot = options.Root;
        await Job("正在扫描文件", async cancellation => { var progress = new Progress<FileProgress>(p => SetStatus($"扫描 {p.Scanned:N0} 文件 · {p.MatchedFiles:N0} 匹配文件 · {p.Hits:N0} 行 · 跳过 {p.Skipped:N0} · {Path.GetFileName(p.Current)}")); var summary = await Task.Run(() => FileSearcher.Search(options, progress, cancellation), CancellationToken.None); _search = summary;
            _hitsGrid.RowCount = summary.Hits.Count; PreviewHit();
            SetStatus($"{(summary.Cancelled ? "已取消" : "搜索完成")} · 扫描 {summary.Scanned:N0} 文件 · {summary.MatchedFiles:N0} 匹配文件 · {summary.Hits.Count:N0} 行 · 跳过 {summary.Skipped:N0}" + (summary.LimitReached ? " · 达到结果行上限" : ""));
            if (summary.Hits.Count == 0) _searchPreview.Text = "未找到匹配结果。" + (summary.Notes.Count > 0 ? "\n\n跳过明细：\n" + string.Join("\n", summary.Notes.Take(10)) : ""); });
    }
    private void PreviewHit()
    {
        if (SelectedHit() is not FileHit hit) return;
        // Only the captured matching line is previewed; selecting a result never loads arbitrary large files.
        _searchPreview.Text = $"{Path.GetRelativePath(_searchRoot, hit.Path)}\n行 {hit.Line}  ·  {hit.Encoding}  ·  本行出现 {hit.Occurrences} 次\n\n{hit.Snippet}";
        int start = _searchPreview.Text.IndexOf(hit.Snippet, StringComparison.Ordinal); if (start >= 0) { _searchPreview.Select(start, hit.Snippet.Length); _searchPreview.SelectionColor = Theme.Accent; _searchPreview.Select(0, 0); }
    }
    private FileHit? SelectedHit() => _search is not null && _hitsGrid.CurrentRow is { Index: >= 0 } row && row.Index < _search.Hits.Count ? _search.Hits[row.Index] : null;
    private async Task OpenText(RichTextBox editor)
    {
        using var dialog = new OpenFileDialog { Filter = "文本文件|*.txt;*.md;*.csv;*.log|所有文件|*.*" }; if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await Job("正在读取文本", async cancellation => { var text = await Task.Run(() => TextFiles.Read(dialog.FileName, maxBytes: 8L * 1024 * 1024, cancellation: cancellation), cancellation); if (text.Text.Length > (editor == _proofInput ? 200_000 : 2_000_000)) throw new InvalidDataException("文件字符数超过当前工作区上限，请分段处理。"); editor.Text = text.Text; SetStatus("已打开 " + Path.GetFileName(dialog.FileName) + " · " + text.Encoding); });
    }
    private async Task Batch()
    {
        using var source = new FolderBrowserDialog { Description = "选择待分词目录（TXT / MD / CSV）" }; if (source.ShowDialog(this) != DialogResult.OK) return;
        using var destination = new FolderBrowserDialog { Description = "选择独立输出目录，原始文件保持只读" }; if (destination.ShowDialog(this) != DialogResult.OK) return; var mode = (SegmentMode)_segmentMode.SelectedIndex;
        await Job("正在批量分词", async cancellation => { var progress = new Progress<FileProgress>(p => SetStatus($"批量分词 {p.Scanned} / {p.MatchedFiles} · {Path.GetFileName(p.Current)}")); var results = await Task.Run(() => BatchSegmenter.Run(_segmenter, source.SelectedPath, destination.SelectedPath, mode, progress: progress, cancellation: cancellation), cancellation); SetStatus($"批量分词完成 · {results.Count} 个文件 · 输出为 UTF-8"); });
    }
    private void SaveText(string text, string filename)
    {
        using var dialog = new SaveFileDialog { FileName = filename, Filter = "文本 / JSON|*.txt;*.json|所有文件|*.*", OverwritePrompt = true }; if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllText(dialog.FileName, text, new UTF8Encoding(false)); SetStatus("已保存 " + Path.GetFileName(dialog.FileName)); } catch (Exception e) { ShowError(e); }
    }
    private void SetStatus(string text) { if (!IsDisposed) _status.Text = text; }
    private void ShowError(Exception e) { _loadingProof = false; SetStatus("未完成 · " + e.Message); MessageBox.Show(this, e.Message, "Text Lab", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    private async Task InitializeShowcase()
    {
        string? showcase = _args.FirstOrDefault(a => a.StartsWith("--showcase", StringComparison.Ordinal)); if (showcase is null) return;
        string mode = showcase.Contains('=') ? showcase.Split('=', 2)[1] : "segment";
        if (mode == "proofread") { Navigate(1); await Proof(); }
        else if (mode == "search")
        {
            string folder = Path.Combine(Path.GetTempPath(), "Liss.TextLab", "showcase"); Directory.CreateDirectory(folder); Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            File.WriteAllText(Path.Combine(folder, "algorithm-notes.md"), "# 算法笔记\n算法将复杂问题拆解成清晰的步骤。\n用动态规划寻找合理切分，用图结构表达关系。\n游戏开发中的算法往往需要性能与可解释性兼顾。", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(folder, "legacy-encoding.txt"), "旧文本使用 GB18030 编码。\n算法的思想可以跨越编程语言。", Encoding.GetEncoding(54936)); File.WriteAllText(Path.Combine(folder, "SegmentExample.cs"), "// 用算法观察文本结构\nvar tokens = segmenter.Cut(text);\n// 分词算法与上下文", new UTF8Encoding(false));
            _path.Text = folder; Navigate(2); await Search();
        }
        else await Segment();
    }
}
