using System.Drawing.Drawing2D;

namespace SolarScope;

internal sealed class MapPickerButton : SolarButton
{
    public MapPickerButton() : base(DesktopLayout.DarkGreen, Color.White, outline: null)
    {
        Name = "btnPickLocation";
        Text = "Pilih di peta";
        Size = new Size(128, 112);
        Font = DesktopLayout.Font(11, bold: true);
        Cursor = Cursors.Hand;
        UseVisualStyleBackColor = false;
    }

    protected override void DrawContent(Graphics graphics, Color textColor)
    {
        int iconSize = LogicalToDeviceUnits(34);
        int iconTop = LogicalToDeviceUnits(20);
        float scale = iconSize / 32f;
        using var pin = new GraphicsPath(FillMode.Alternate);
        pin.AddBezier(16, 30, 13, 26, 6, 19, 6, 12);
        pin.AddBezier(6, 12, 6, -1, 26, -1, 26, 12);
        pin.AddBezier(26, 12, 26, 19, 19, 26, 16, 30);
        pin.CloseFigure();
        pin.AddEllipse(12, 8, 8, 8);
        using var position = new Matrix(scale, 0, 0, scale, (Width - iconSize) / 2f, iconTop);
        pin.Transform(position);
        using var brush = new SolidBrush(Enabled ? SolarTheme.Yellow : textColor);
        graphics.FillPath(brush, pin);

        int textTop = iconTop + iconSize + LogicalToDeviceUnits(7);
        var textBounds = new Rectangle(0, textTop, Width, LogicalToDeviceUnits(24));
        TextRenderer.DrawText(graphics, Text, Font, textBounds, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
