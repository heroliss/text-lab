using System.Drawing.Drawing2D;

namespace Liss.TextLab.App;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(10, 16, 25), Surface = Color.FromArgb(17, 27, 40), Raised = Color.FromArgb(23, 36, 52),
        Border = Color.FromArgb(44, 62, 82), Text = Color.FromArgb(235, 242, 249), Muted = Color.FromArgb(161, 179, 198), Accent = Color.FromArgb(81, 224, 203),
        Purple = Color.FromArgb(189, 159, 255), Amber = Color.FromArgb(247, 190, 97), Blue = Color.FromArgb(119, 179, 255);
    public static Font Font(float size = 10, FontStyle style = FontStyle.Regular) => new("Microsoft YaHei UI", size, style);
    public static Label Label(string text, float size = 10, Color? color = null, FontStyle style = FontStyle.Regular) => new() { Text = text, ForeColor = color ?? Text, Font = Font(size, style), AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    public static Button Button(string text, bool primary = false, int width = 110)
    {
        var button = new Button { Text = text, Width = width, Height = 38, FlatStyle = FlatStyle.Flat, BackColor = primary ? Accent : Raised, ForeColor = primary ? Background : Text,
            Font = Font(9.5f, FontStyle.Bold), Margin = new Padding(0, 4, 8, 4), Cursor = Cursors.Hand, UseVisualStyleBackColor = false };
        button.FlatAppearance.BorderColor = primary ? Accent : Border; button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(119, 240, 223) : Color.FromArgb(38, 54, 73); return button;
    }
    public static TextBox Input(string text = "") => new() { Text = text, BackColor = Raised, ForeColor = Text, BorderStyle = BorderStyle.FixedSingle, Font = Font(10), Dock = DockStyle.Fill, Margin = new Padding(0, 7, 8, 7) };
    public static RichTextBox Editor(string text = "", bool readOnly = false) => new() { Text = text, ReadOnly = readOnly, BackColor = Surface, ForeColor = Text, BorderStyle = BorderStyle.None,
        Font = Font(12), Dock = DockStyle.Fill, DetectUrls = false, HideSelection = false, Margin = new Padding(12), WordWrap = true };
    public static ComboBox Combo(params string[] choices)
    {
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Raised, ForeColor = Text, FlatStyle = FlatStyle.Flat, Font = Font(10), Width = 160, Margin = new Padding(0, 7, 8, 6) };
        combo.Items.AddRange(choices); combo.SelectedIndex = 0; return combo;
    }
    public static DataGridView Grid(params (string Name, string Header, float Width)[] columns)
    {
        var grid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Surface, BorderStyle = BorderStyle.None, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
            ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, EnableHeadersVisualStyles = false,
            GridColor = Border, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, Font = Font(9.5f), RowTemplate = { Height = 34 } };
        grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Surface, ForeColor = Text, SelectionBackColor = Raised, SelectionForeColor = Accent, Padding = new Padding(8, 3, 4, 3) };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(19, 30, 44) };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Raised, ForeColor = Muted, Font = Font(9, FontStyle.Bold), Padding = new Padding(8) };
        grid.ColumnHeadersHeight = 38;
        foreach (var column in columns) grid.Columns.Add(new DataGridViewTextBoxColumn { Name = column.Name, HeaderText = column.Header, FillWeight = column.Width, SortMode = DataGridViewColumnSortMode.NotSortable });
        return grid;
    }
    public static Panel Card(string title, Control content, string? caption = null)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(16), Margin = new Padding(0, 0, 12, 12) };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = caption is null ? 2 : 3, BackColor = Surface };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(Label(title, 11, Text, FontStyle.Bold), 0, 0); table.Controls.Add(content, 0, 1);
        if (caption is not null) { table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); table.Controls.Add(Label(caption, 8.5f, Muted), 0, 2); }
        panel.Controls.Add(table); return panel;
    }
    public static FlowLayoutPanel Toolbar() => new() { Dock = DockStyle.Fill, BackColor = Background, WrapContents = false, Padding = new Padding(0, 2, 0, 2) };
}

internal sealed class FrequencyBars : Control
{
    private IReadOnlyList<(string Word, int Count)> _items = [];
    public FrequencyBars() { DoubleBuffered = true; Dock = DockStyle.Fill; BackColor = Theme.Surface; }
    public void SetItems(IEnumerable<(string Word, int Count)> items) { _items = items.Take(8).ToArray(); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; using var font = Theme.Font(9); using var muted = new SolidBrush(Theme.Muted); using var text = new SolidBrush(Theme.Text);
        if (_items.Count == 0) { g.DrawString("分词后显示高频词分布", font, muted, 8, 10); return; }
        float row = Math.Min(30, Height / (float)_items.Count); int max = _items.Max(x => x.Count); int labelWidth = Math.Min(120, Width / 3);
        for (int i = 0; i < _items.Count; i++)
        {
            float y = i * row + 3; g.DrawString(_items[i].Word, font, text, new RectangleF(0, y, labelWidth, row));
            using var bg = new SolidBrush(Theme.Raised); using var fill = new SolidBrush(i % 2 == 0 ? Theme.Accent : Theme.Blue);
            float w = Math.Max(1, Width - labelWidth - 34); g.FillRectangle(bg, labelWidth, y + 3, w, 12); g.FillRectangle(fill, labelWidth, y + 3, w * _items[i].Count / max, 12);
            g.DrawString(_items[i].Count.ToString(), font, muted, Width - 28, y);
        }
    }
}
