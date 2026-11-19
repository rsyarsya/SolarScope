using SolarScope.Core.Models;
using SolarScope.Core.Services;

namespace SolarScope;

public enum Screen { Home, Input, Result, History, Comparison, Guide }

public sealed class AppController : ApplicationContext
{
    public AppSession Session { get; } = new();
    public SimulationManager Simulations { get; }
    public ISimulationRepository History { get; }
    internal HttpClient MapHttpClient { get; }
    private readonly HttpClient? ownedHttpClient;
    private readonly PostgresSimulationRepository? ownedRepository;
    private readonly Form window;
    private readonly Panel pageHost;
    private readonly NavigationRail navigation;
    private readonly Icon windowIcon = SolarTheme.LoadAppIcon();
    public Form ActivePage { get; private set; } = null!;
    private Screen resultOrigin = Screen.Input;
    private SimulationResult? shownResult;
    private bool writingHistory;

    public AppController(HttpClient? httpClient = null, ISimulationRepository? history = null)
    {
        if (httpClient is null)
        {
            ownedHttpClient = httpClient = new HttpClient();
        }
        Simulations = new SimulationManager(new NasaPowerClient(httpClient));
        MapHttpClient = httpClient;
        if (history is null)
        {
            var connection = Environment.GetEnvironmentVariable(PostgresSimulationRepository.ConnectionEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(connection))
            {
                history = new UnavailableSimulationRepository("Koneksi database belum diatur. Atur koneksi dan buka ulang SolarScope.");
            }
            else
            {
                try
                {
                    history = ownedRepository = new PostgresSimulationRepository(connection);
                }
                catch (Exception error) when (error is ArgumentException or NotSupportedException)
                {
                    history = new UnavailableSimulationRepository("Konfigurasi database tidak valid. Periksa konfigurasi dan buka ulang SolarScope.");
                }
            }
        }
        History = history;
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
        window.FormClosing += (_, args) =>
        {
            if (writingHistory && args.CloseReason == CloseReason.UserClosing) args.Cancel = true;
        };
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
            ownedRepository?.Dispose();
        }
    }

    internal async Task<ServiceResult<T>> WriteHistoryAsync<T>(Func<Task<ServiceResult<T>>> operation)
    {
        if (writingHistory) return ServiceResult<T>.Fail("Tunggu operasi riwayat selesai.");
        writingHistory = true;
        navigation.Enabled = false;
        try
        {
            return await operation();
        }
        finally
        {
            writingHistory = false;
            if (!navigation.IsDisposed) navigation.Enabled = true;
        }
    }

    public void ShowResult(SimulationResult result, Screen origin)
    {
        shownResult = result;
        resultOrigin = origin;
        Navigate(Screen.Result);
    }

    internal void ForgetDeletedSimulation(int id)
    {
        Session.ForgetResult(id);
        if (shownResult?.Estimate.SimulationId == id) shownResult = null;
    }

    public void Navigate(Screen target)
    {
        if (writingHistory) return;
        var previous = ActivePage;
        Form next = target switch {
            Screen.Home => new BerandaForm(this),
            Screen.Input => new Form1(this),
            Screen.History => new RiwayatForm(this),
            Screen.Comparison => new PerbandinganForm(this, shownResult?.Estimate ?? Session.CurrentResult?.Estimate),
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
