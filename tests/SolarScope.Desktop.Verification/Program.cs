using System.Diagnostics;
using System.Net;
using SolarScope;
using System.Windows.Forms;
using Screen = SolarScope.Screen;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Console.WriteLine("PASS " + message); checks++;
    }
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var session = new AppSession();
        Check(session.History.Count == 0, "New session has empty history");
        Check(DemoData.Scenarios.Select(s => s.Capacity).SequenceEqual(new decimal[]{2,3,5}), "Comparison has three approved capacity fixtures");
        foreach(var scenario in DemoData.Scenarios)
        {
            Check(scenario.Months.Count == 12 && scenario.Months.All(m => m.Energy > 0), $"Scenario {scenario.Capacity} has twelve positive monthly values");
            Check(scenario.AnnualTotal == scenario.Months.Sum(m => m.Energy), $"Scenario {scenario.Capacity} total equals displayed months");
        }
        Check(!DemoData.ValidComparison(0) && !DemoData.ValidComparison(1) && DemoData.ValidComparison(2) && DemoData.ValidComparison(3) && !DemoData.ValidComparison(4), "Comparison accepts exactly two or three choices");

        bool live = args.Contains("--nasa-live");
        var handler = new NasaHandler();
        using var httpClient = live ? new HttpClient() : new HttpClient(handler);
        using var controller = new AppController(httpClient);
        Exception? failure = null;
        controller.MainForm!.BeginInvoke(() =>
        {
            try { VerifyDesktop(controller, handler, live); }
            catch (Exception error)
            {
                failure = error;
                controller.MainForm?.Close();
            }
        });
        Application.Run(controller);
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(failure);
        Console.WriteLine($"TOTAL {checks} checks passed.");
    }

    private static void VerifyDesktop(AppController controller, NasaHandler handler, bool live)
    {
        controller.Session.Draft = new("Draft tetap", "Yogyakarta", 3);
        for(int cycle=0; cycle<3; cycle++)
        {
            foreach(var screen in new[]{Screen.Input,Screen.History,Screen.Comparison,Screen.Guide,Screen.Home})
            {
                var previous = controller.MainForm!;
                controller.Navigate(screen);
                Application.DoEvents();
                Check(previous.IsDisposed && Application.OpenForms.Count == 1, $"Cycle {cycle+1}, {screen}: previous disposed and one form open");
            }
        }
        Check(controller.Session.Draft.Name == "Draft tetap", "Draft survives repeated navigation");
        controller.Navigate(Screen.Input);
        var inputForm = controller.MainForm!;
        var name = (TextBox)inputForm.Controls.Find("tbNamaSimulasi",true).Single();
        var location = (TextBox)inputForm.Controls.Find("tbLokasi",true).Single();
        var latitude = (TextBox)inputForm.Controls.Find("tbLatitude",true).Single();
        var longitude = (TextBox)inputForm.Controls.Find("tbLongitude",true).Single();
        var capacity = (NumericUpDown)inputForm.Controls.Find("nudKapasitas",true).Single();
        var preview = (Button)inputForm.Controls.Find("btnLihatHasil",true).Single();
        name.Text="  "; preview.PerformClick();
        Check(controller.MainForm == inputForm && inputForm.Controls.Find("errorNama",true).Single().Text.Length>0, "Whitespace name is rejected inline");
        name.Text="Draft tetap"; location.Text=""; preview.PerformClick();
        Check(controller.MainForm == inputForm && inputForm.Controls.Find("errorLokasi",true).Single().Text.Length>0, "Empty location is rejected inline");
        location.Text="Yogyakarta"; latitude.Text="-91"; longitude.Text="110.3695"; preview.PerformClick();
        Check(controller.MainForm == inputForm && inputForm.Controls.Find("errorKoordinat",true).Single().Text.Contains("Latitude") && handler.RequestCount == 0,
            "Out-of-range coordinates are rejected before requesting data");
        latitude.Text="-7,7956"; capacity.Value=0; preview.PerformClick();
        Check(controller.MainForm == inputForm && inputForm.Controls.Find("errorKapasitas",true).Single().Text.Length>0, "Zero capacity is rejected inline");
        capacity.Value=2;
        if (!live)
        {
            preview.PerformClick();
            Check(!preview.Enabled && !latitude.Enabled && inputForm.Controls.Find("notice", true).Single().Text.Contains("Mengambil"), "Form shows loading and prevents duplicate submission");
            preview.PerformClick();
            Check(handler.RequestCount == 1, "Only one request is sent while loading");
            handler.Complete(HttpStatusCode.ServiceUnavailable);
            WaitFor(() => preview.Enabled);
            Check(controller.MainForm == inputForm && inputForm.Controls.Find("notice", true).Single().Text.Contains("503"),
                "HTTP failure stays on input and enables retry");
        }
        preview.PerformClick();
        if (!live) handler.Complete(HttpStatusCode.OK);
        WaitFor(() => controller.MainForm is HasilForm { Visible: true } && inputForm.IsDisposed);
        Check(controller.MainForm is HasilForm, "Calculated result opens");
        var first = controller.Session.CurrentResult!;
        var results = controller.MainForm!;
        var grid = (DataGridView)results.Controls.Find("dgvBulanan",true).Single();
        Check(grid.Rows.Count == 12 && grid.ReadOnly, "Result UI displays twelve read-only rows");
        var firstTotal = first.Estimate.Summary!.TotalAnnualEnergyKwh;
        Check(live ? firstTotal > 0 : firstTotal == 2737.5, "Result uses calculated energy from NASA readings");
        Check(results.Controls.Find("lblTotalValue", true).Single().Text == $"{firstTotal.ToString("N2", DemoData.Culture)} kWh",
            "Result shows the calculated total to two decimal places");
        ((Button)results.Controls.Find("btnSave",true).Single()).PerformClick();
        Check(!results.Controls.Find("btnSave",true).Single().Enabled && controller.Session.History.Count == 1, "Save button reflects saved state");
        ((Button)results.Controls.Find("btnKembali",true).Single()).PerformClick();
        Check(controller.MainForm is Form1, "Result back returns to Input");
        inputForm = controller.MainForm!;
        Check(inputForm.Controls.Find("tbLatitude", true).Single().Text == "-7,7956", "Coordinates survive returning to the input form");
        ((Button)inputForm.Controls.Find("btnLihatHasil", true).Single()).PerformClick();
        Check(controller.MainForm is HasilForm && controller.Session.CurrentResult?.Id == first.Id, "Unchanged input preserves result identity");
        controller.Navigate(Screen.Input);
        inputForm = controller.MainForm!;
        ((NumericUpDown)inputForm.Controls.Find("nudKapasitas", true).Single()).Value = 4;
        ((Button)inputForm.Controls.Find("btnLihatHasil", true).Single()).PerformClick();
        if (!live) handler.Complete(HttpStatusCode.OK);
        WaitFor(() => controller.MainForm is HasilForm { Visible: true } && inputForm.IsDisposed);
        var second = controller.Session.CurrentResult!;
        Check(second.Id != first.Id && Math.Abs(second.Estimate.Summary!.TotalAnnualEnergyKwh - 2 * firstTotal) <= 0.120001,
            "Doubling capacity approximately doubles the estimate");
        Check(controller.Session.Save(second) && !controller.Session.Save(second) && controller.Session.History.Count == 2, "Duplicate save is prevented");
        Check(controller.Session.History[1].Result.Input.Capacity == 2, "Saved input preserves the original capacity");
        Check(controller.Session.Delete(second.Id) && !controller.Session.Delete(second.Id) && controller.Session.History.Count == 1, "Delete affects the selected entry only");
        controller.ShowResult(first, Screen.History);
        Check(!controller.MainForm!.Controls.Find("btnEdit",true).Single().Visible, "Saved detail hides edit action");
        ((Button)controller.MainForm.Controls.Find("btnKembali",true).Single()).PerformClick();
        Check(controller.MainForm is RiwayatForm, "Saved detail back returns to History");
        controller.Navigate(Screen.Comparison);
        var comparison = controller.MainForm!;
        var comparisonGrid = (DataGridView)comparison.Controls.Find("dgvComparison",true).Single();
        Check(comparisonGrid.Rows.Count == 12 && comparisonGrid.Columns.Count == 4, "Three-way comparison UI has twelve months and three capacity columns");
        ((CheckBox)comparison.Controls.Find("chk5",true).Single()).Checked=false;
        ((Button)comparison.Controls.Find("btnCompare",true).Single()).PerformClick();
        Check(comparisonGrid.Rows.Count == 12 && comparisonGrid.Columns.Count == 3, "Two-way comparison updates table");
        ((CheckBox)comparison.Controls.Find("chk3",true).Single()).Checked=false;
        ((Button)comparison.Controls.Find("btnCompare",true).Single()).PerformClick();
        Check(comparisonGrid.Rows.Count == 0 && comparison.Controls.Find("lblValidation",true).Single().Text.Contains("minimal dua"), "One choice produces clear validation without stale data");
        controller.MainForm.Close();
        Application.DoEvents();
        Check(Application.OpenForms.Count == 0, "Closing active form leaves no hidden windows");
    }

    private static void WaitFor(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition() && elapsed.Elapsed < TimeSpan.FromSeconds(18))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        if (!condition()) throw new TimeoutException("The desktop did not reach the expected state.");
    }

    private sealed class NasaHandler : HttpMessageHandler
    {
        private TaskCompletionSource<HttpResponseMessage> pending = null!;
        internal int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return pending.Task.WaitAsync(cancellationToken);
        }

        internal void Complete(HttpStatusCode status)
        {
            const string data = """
                {
                  "properties": { "parameter": { "ALLSKY_SFC_SW_DWN": {
                    "JAN": 5, "FEB": 5, "MAR": 5, "APR": 5, "MAY": 5, "JUN": 5,
                    "JUL": 5, "AUG": 5, "SEP": 5, "OCT": 5, "NOV": 5, "DEC": 5
                  } } },
                  "parameters": { "ALLSKY_SFC_SW_DWN": { "units": "kW-hr/m^2/day" } }
                }
                """;
            pending.SetResult(new HttpResponseMessage(status) { Content = new StringContent(data) });
        }
    }
}
