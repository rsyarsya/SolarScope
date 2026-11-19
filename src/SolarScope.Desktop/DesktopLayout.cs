using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SolarScope;

internal static class DesktopLayout
{
    internal static readonly Color Canvas = ColorTranslator.FromHtml("#F7F8F4");
    internal static readonly Color Rail = ColorTranslator.FromHtml("#F1F4EB");
    internal static readonly Color Table = ColorTranslator.FromHtml("#F2F5EF");
    internal static readonly Color Soft = ColorTranslator.FromHtml("#EBF4E8");
    internal static readonly Color ControlBorder = ColorTranslator.FromHtml("#788779");
    internal static readonly Color DarkGreen = ColorTranslator.FromHtml("#234C1C");
    internal static readonly Color Error = ColorTranslator.FromHtml("#A12B22");
    internal static readonly Color ErrorSurface = ColorTranslator.FromHtml("#FFF3F0");
    internal static readonly Color Disabled = ColorTranslator.FromHtml("#E7ECE4");
    private static readonly ConcurrentDictionary<(float Points, bool Bold), Font> Fonts = new();

    internal static Font Font(float points, bool bold = false) =>
        Fonts.GetOrAdd((points, bold), static key =>
            new Font(key.Bold ? "Segoe UI Semibold" : "Segoe UI", key.Points, FontStyle.Regular, GraphicsUnit.Point));

    internal static Label Text(string text, float points = 11, bool bold = false, Color? ink = null, string name = "") =>
        new() { Name = name, Text = text, AutoSize = true, Font = Font(points, bold),
            ForeColor = ink ?? SolarTheme.Ink, Margin = Padding.Empty };

    internal static Label Title(string text) => Text(text, 25, true);
    internal static Label Note(string text) => Text(text, 9.5f, false, SolarTheme.MutedInk);

    internal static Button Button(string name, string text, bool primary = false, bool danger = false) =>
        new SolarButton(primary ? SolarTheme.Green : danger ? Error : Color.White,
            primary || danger ? Color.White : SolarTheme.Ink, primary || danger ? null : ControlBorder)
        {
            Name = name, Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(15, 0, 15, 0), MinimumSize = new Size(0, 38), Font = Font(10.5f, true),
            Margin = Padding.Empty, UseVisualStyleBackColor = false
        };

    internal static TableLayoutPanel Stack(int gap, params Control[] controls)
    {
        var stack = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1,
            Dock = DockStyle.Top, Margin = Padding.Empty, GrowStyle = TableLayoutPanelGrowStyle.AddRows
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < controls.Length; i++)
        {
            var control = controls[i];
            control.Margin = new Padding(0, i == 0 ? 0 : gap, 0, 0);
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.Controls.Add(control, 0, i);
        }
        return stack;
    }

    internal static FlowLayoutPanel Actions(params Control[] controls)
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
            Margin = Padding.Empty
        };
        foreach (var control in controls)
        {
            control.Margin = new Padding(flow.Controls.Count == 0 ? 0 : 8, 0, 0, 0);
            flow.Controls.Add(control);
        }
        return flow;
    }

    internal static TableLayoutPanel Across(Control left, Control right, bool balance = false)
    {
        var row = new TableLayoutPanel
        {
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = 1,
            Dock = DockStyle.Top, Margin = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, balance ? 50 : 100));
        row.ColumnStyles.Add(balance ? new ColumnStyle(SizeType.Percent, 50) : new ColumnStyle(SizeType.AutoSize));
        left.Margin = Padding.Empty;
        right.Margin = balance ? new Padding(12, 0, 0, 0) : Padding.Empty;
        if (!balance)
        {
            left.Anchor = AnchorStyles.Left;
            right.Anchor = AnchorStyles.Right;
        }
        row.Controls.Add(left, 0, 0);
        row.Controls.Add(right, 1, 0);
        return row;
    }

    internal static Panel Card(Control content, int width = 0, int padding = 24)
    {
        var card = new RoundedPanel
        {
            Fill = Color.White, Outline = SolarTheme.Border, Padding = new Padding(padding),
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MaximumSize = width == 0 ? Size.Empty : new Size(width, 0), Dock = DockStyle.Top
        };
        content.Dock = DockStyle.Top;
        card.Controls.Add(content);
        return card;
    }

    internal static void Page(Form page, string title, Control stack)
    {
        page.SuspendLayout();
        page.Text = title;
        page.Font = Font(11);
        page.AutoScaleDimensions = new SizeF(96, 96);
        page.AutoScaleMode = AutoScaleMode.Dpi;
        page.ClientSize = new Size(916, 728);
        page.BackColor = Canvas;
        var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(32, 22, 32, 22) };
        viewport.Controls.Add(stack);
        page.Controls.Add(viewport);
        page.ResumeLayout(true);
    }

    internal static Panel Rule() => new() { Height = 1, Dock = DockStyle.Top, BackColor = SolarTheme.Border };

    internal static DataGridView Grid(string name, int rowHeight, bool select = false) => new SolarGrid()
    {
        Name = name, Dock = DockStyle.Top, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = SolarTheme.Border,
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single, EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 32, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            Font = Font(10, true), BackColor = Table, ForeColor = SolarTheme.MutedInk,
            SelectionBackColor = Table, SelectionForeColor = SolarTheme.MutedInk, Padding = new Padding(12, 0, 12, 0)
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = Font(10), BackColor = Color.White, ForeColor = SolarTheme.Ink,
            SelectionBackColor = select ? SolarTheme.LightGreen : Color.White,
            SelectionForeColor = select ? DarkGreen : SolarTheme.Ink, Padding = new Padding(12, 0, 12, 0)
        },
        RowTemplate = { Height = rowHeight }, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, AllowUserToResizeColumns = false, ReadOnly = true,
        MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, Height = 34,
        AccessibleName = name switch
        {
            "dgvBulanan" => "Estimasi energi bulanan", "dgvHistory" => "Riwayat simulasi",
            "dgvComparison" => "Perbandingan estimasi bulanan", _ => "Simulasi terakhir"
        }
    };

    internal static void Column(DataGridView grid, string key, string title, bool numeric = false)
    {
        var column = new DataGridViewTextBoxColumn { Name = key, HeaderText = title, SortMode = DataGridViewColumnSortMode.NotSortable };
        if (numeric)
        {
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        grid.Columns.Add(column);
    }

    internal static void SizeGrid(DataGridView grid, int maximumRows = 12) =>
        grid.Height = grid.ColumnHeadersHeight + Math.Min(maximumRows, grid.Rows.Count) * grid.RowTemplate.Height + 2;

    internal static GraphicsPath Rounded(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class SolarGrid : DataGridView
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(SolarTheme.Border);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }
}

internal class RoundedPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Color Fill { get; set; } = Color.White;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Color Outline { get; set; } = Color.Transparent;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int OutlineWidth { get; set; } = 1;

    public RoundedPanel()
    {
        BackColor = Color.Transparent;
        DoubleBuffered = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        const int radius = 7;
        if (Width < radius * 2 || Height < radius * 2) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var shape = DesktopLayout.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), radius);
        using var fill = new SolidBrush(Fill);
        using var outline = new Pen(Outline, OutlineWidth) { Alignment = PenAlignment.Inset };
        e.Graphics.FillPath(fill, shape);
        e.Graphics.DrawPath(outline, shape);
    }
}

internal class SolarButton(Color background, Color ink, Color? outline) : Button
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
        if (Width < 16 || Height < 16) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var shape = DesktopLayout.Rounded(new Rectangle(1, 1, Width - 3, Height - 3), 6);
        using var fill = new SolidBrush(Enabled ? background : DesktopLayout.Disabled);
        e.Graphics.FillPath(fill, shape);
        if (Enabled && outline is Color border)
        {
            using var pen = new Pen(border);
            e.Graphics.DrawPath(pen, shape);
        }
        DrawContent(e.Graphics, Enabled ? ink : SolarTheme.MutedInk);
        if (Enabled && (Focused || IsDefault))
        {
            var focus = background == DesktopLayout.DarkGreen || (Focused && background == SolarTheme.Green)
                ? Color.White : SolarTheme.Green;
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4), focus, background);
        }
    }

    protected virtual void DrawContent(Graphics graphics, Color textColor) =>
        TextRenderer.DrawText(graphics, Text, Font, ClientRectangle, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
}
