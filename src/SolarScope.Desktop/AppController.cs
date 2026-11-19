using SolarScope.Core.Services;

namespace SolarScope;

public enum Screen { Home, Input, Result, History, Comparison, Guide }

public sealed class AppController : ApplicationContext
{
    public AppSession Session { get; } = new();
    public SimulationManager Simulations { get; }
    private readonly HttpClient? ownedHttpClient;
    private bool navigating;
    private Screen resultOrigin = Screen.Input;
    private SimulationResult? shownResult;

    public AppController(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            ownedHttpClient = httpClient = new HttpClient();
        }
        Simulations = new SimulationManager(new NasaPowerClient(httpClient));
        Navigate(Screen.Home);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) ownedHttpClient?.Dispose();
    }

    public void ShowResult(SimulationResult result, Screen origin)
    {
        shownResult = result;
        resultOrigin = origin;
        Navigate(Screen.Result);
    }

    public void Navigate(Screen target)
    {
        var previous = MainForm;
        Form next = target switch {
            Screen.Home => new BerandaForm(this),
            Screen.Input => new Form1(this),
            Screen.History => new RiwayatForm(this),
            Screen.Comparison => new PerbandinganForm(this),
            Screen.Guide => new PanduanForm(this),
            Screen.Result when shownResult is not null => new HasilForm(this, shownResult, resultOrigin),
            Screen.Result => throw new InvalidOperationException("Belum ada hasil simulasi."),
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
        if (previous != null)
        {
            next.StartPosition = FormStartPosition.Manual;
            next.Bounds = previous.WindowState == FormWindowState.Normal ? previous.Bounds : previous.RestoreBounds;
            next.WindowState = previous.WindowState;
        }
        next.FormClosing += (_, _) => { if (!navigating) ExitThread(); };
        navigating = true;
        MainForm = next;
        previous?.Hide();
        next.Show();
        previous?.Close();
        previous?.Dispose();
        navigating = false;
    }
}
