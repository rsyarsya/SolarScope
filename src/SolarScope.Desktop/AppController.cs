using SolarScope.Core.Services;

namespace SolarScope;

public enum Screen { Home, Input, Result, History, Comparison, Guide }

public sealed class AppController : ApplicationContext
{
    public AppSession Session { get; } = new();
    public SimulationManager Simulations { get; }
    private readonly HttpClient? ownedHttpClient;
    private readonly Form window;
    private readonly Panel pageHost;
    private readonly NavigationRail navigation;
    private readonly Icon windowIcon = SolarTheme.LoadAppIcon();
    public Form ActivePage { get; private set; } = null!;
    private Screen resultOrigin = Screen.Input;
    private SimulationResult? shownResult;

    public AppController(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            ownedHttpClient = httpClient = new HttpClient();
        }
        Simulations = new SimulationManager(new NasaPowerClient(httpClient));
        window = new Form
        {
            Size = new Size(1100, 760),
            MinimumSize = new Size(960, 680),
            AutoScaleDimensions = new SizeF(96F, 96F),
            AutoScaleMode = AutoScaleMode.Dpi,
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = DesktopLayout.Canvas,
            Icon = windowIcon,
            Text = "SolarScope"
        };
        pageHost = new Panel { Dock = DockStyle.Fill, Name = "pageHost", TabIndex = 1 };
        navigation = new NavigationRail(Navigate) { TabIndex = 0 };
        window.Controls.Add(pageHost);
        window.Controls.Add(navigation);
        window.ClientSizeChanged += (_, _) => navigation.Width = window.LogicalToDeviceUnits(
            window.ClientSize.Width <= window.LogicalToDeviceUnits(980) ? 176 : 184);
        MainForm = window;
        Navigate(Screen.Home);
        window.Show();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            windowIcon.Dispose();
            ownedHttpClient?.Dispose();
        }
    }

    public void ShowResult(SimulationResult result, Screen origin)
    {
        shownResult = result;
        resultOrigin = origin;
        Navigate(Screen.Result);
    }

    public void Navigate(Screen target)
    {
        var previous = ActivePage;
        Form next = target switch {
            Screen.Home => new BerandaForm(this),
            Screen.Input => new Form1(this),
            Screen.History => new RiwayatForm(this),
            Screen.Comparison => new PerbandinganForm(this),
            Screen.Guide => new PanduanForm(),
            Screen.Result when shownResult is not null => new HasilForm(this, shownResult, resultOrigin),
            Screen.Result => throw new InvalidOperationException("Belum ada hasil simulasi."),
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
        next.TopLevel = false;
        next.FormBorderStyle = FormBorderStyle.None;
        next.MinimumSize = Size.Empty;
        next.Dock = DockStyle.Fill;

        window.SuspendLayout();
        try
        {
            pageHost.Controls.Add(next);
            next.BringToFront();
            ActivePage = next;
            navigation.SetActive(target == Screen.Result ? resultOrigin : target);
            next.Show();
            previous?.Dispose();
        }
        finally
        {
            window.ResumeLayout(performLayout: true);
        }
    }
}
