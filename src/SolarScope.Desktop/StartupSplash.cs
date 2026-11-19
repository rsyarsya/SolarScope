using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace SolarScope;

internal sealed class StartupSplash : Form
{
    private const double DurationMs = 850;
    private readonly Stopwatch elapsed = new();
    private readonly System.Windows.Forms.Timer animation = new() { Interval = 16 };

    public StartupSplash()
    {
        Text = "SolarScope";
        AccessibleName = "SolarScope sedang dibuka";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(420, 184);
        BackColor = DesktopLayout.Canvas;
        DoubleBuffered = true;
        animation.Tick += (_, _) =>
        {
            if (elapsed.Elapsed.TotalMilliseconds >= DurationMs)
                Close();
            else
                Invalidate();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        elapsed.Start();
        animation.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int logoWidth = LogicalToDeviceUnits(242);
        var logo = SolarTheme.Logo;
        int logoHeight = logoWidth * logo.Height / logo.Width;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.DrawImage(logo, (ClientSize.Width - logoWidth) / 2,
            LogicalToDeviceUnits(35), logoWidth, logoHeight);

        int trackWidth = LogicalToDeviceUnits(104);
        int height = LogicalToDeviceUnits(4);
        int segmentWidth = LogicalToDeviceUnits(30);
        int left = (ClientSize.Width - trackWidth) / 2;
        int top = LogicalToDeviceUnits(133);
        double phase = Math.Clamp(elapsed.Elapsed.TotalMilliseconds / DurationMs, 0, 1);
        int offset = (int)((trackWidth - segmentWidth) * (0.5 - 0.5 * Math.Cos(phase * Math.PI)));
        using var track = new SolidBrush(SolarTheme.Border);
        using var segment = new SolidBrush(SolarTheme.Green);
        e.Graphics.FillRectangle(track, left, top, trackWidth, height);
        e.Graphics.FillRectangle(segment, left + offset, top, segmentWidth, height);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) animation.Dispose();
        base.Dispose(disposing);
    }
}
