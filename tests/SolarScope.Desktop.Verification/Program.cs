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
        if (args.Contains("--map"))
        {
            MapLocationVerification.Run();
            return;
        }
        bool live = args.Contains("--nasa-live");
        var handler = new NasaHandler();
        using var httpClient = live ? new HttpClient() : new HttpClient(handler);
        var history = new TestSimulationRepository();
        using var controller = new AppController(httpClient, history);
        Exception? failure = null;
        controller.MainForm!.BeginInvoke(() =>
        {
            try { VerifyDesktop(controller, handler, history, live); }
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

    private static void VerifyDesktop(AppController controller, NasaHandler handler, TestSimulationRepository history, bool live)
    {
        var window = controller.MainForm!;
        var handle = window.Handle;
        var host = window.Controls.Find("pageHost", false).Single();
        var rail = window.Controls.Find("navigationRail", false).Single();
        window.ClientSize = new System.Drawing.Size(1024, 700);
        var bounds = window.Bounds;
        controller.Session.Draft = new("Draft tetap", "Yogyakarta", 3);
        for(int cycle=0; cycle<3; cycle++)
        {
            foreach(var screen in new[]{Screen.Input,Screen.History,Screen.Comparison,Screen.Guide,Screen.Home})
            {
                var previous = controller.ActivePage!;
                controller.Navigate(screen);
                Application.DoEvents();
                Check(previous.IsDisposed && controller.ActivePage.Parent == host && host.Controls.Count == 1
                    && rail.Parent == window && rail.Visible && window.Controls.Count == 2
                    && controller.MainForm == window && window.Handle == handle
                    && window.Bounds == bounds && Application.OpenForms.Cast<Form>().Count(form => form.TopLevel) == 1,
                    $"Cycle {cycle+1}, {screen}: page changes inside the same window");
            }
        }
        Check(controller.Session.Draft.Name == "Draft tetap", "Draft survives repeated navigation");
        controller.Navigate(Screen.Comparison);
        Check(controller.ActivePage.Controls.Find("btnCreateComparisonSource", true).Single().Visible
            && controller.ActivePage.Controls.Find("btnOpenHistory", true).Single().Visible
            && !controller.ActivePage.Controls.Find("dgvComparison", true).Single().Visible,
            "Comparison without a source offers simulation and history instead of sample data");
        controller.Navigate(Screen.Input);
        var inputForm = controller.ActivePage!;
        var name = (TextBox)inputForm.Controls.Find("tbNamaSimulasi",true).Single();
        var location = (TextBox)inputForm.Controls.Find("tbLokasi",true).Single();
        var latitude = (TextBox)inputForm.Controls.Find("tbLatitude",true).Single();
        var longitude = (TextBox)inputForm.Controls.Find("tbLongitude",true).Single();
        var capacity = (NumericUpDown)inputForm.Controls.Find("nudKapasitas",true).Single();
        var preview = (Button)inputForm.Controls.Find("btnLihatHasil",true).Single();
        name.Text="  "; preview.PerformClick();
        Check(controller.ActivePage == inputForm && inputForm.Controls.Find("errorNama",true).Single().Text.Length>0, "Whitespace name is rejected inline");
        name.Text="Draft tetap"; location.Text=""; preview.PerformClick();
        Check(controller.ActivePage == inputForm && inputForm.Controls.Find("errorLokasi",true).Single().Text.Length>0, "Empty location is rejected inline");
        location.Text="Yogyakarta"; latitude.Text="-91"; longitude.Text="110.3695"; preview.PerformClick();
        Check(controller.ActivePage == inputForm && inputForm.Controls.Find("errorKoordinat",true).Single().Text.Contains("Latitude") && handler.RequestCount == 0,
            "Out-of-range coordinates are rejected before requesting data");
        latitude.Text="-7,7956"; capacity.Value=0; preview.PerformClick();
        Check(controller.ActivePage == inputForm && inputForm.Controls.Find("errorKapasitas",true).Single().Text.Length>0, "Zero capacity is rejected inline");
        capacity.Value=2;
        if (!live)
        {
            preview.PerformClick();
            Check(!preview.Enabled && !latitude.Enabled && inputForm.Controls.Find("notice", true).Single().Text.Contains("Mengambil"), "Form shows loading and prevents duplicate submission");
            preview.PerformClick();
            Check(handler.RequestCount == 1, "Only one request is sent while loading");
            handler.Complete(HttpStatusCode.ServiceUnavailable);
            WaitFor(() => preview.Enabled);
            Check(controller.ActivePage == inputForm && inputForm.Controls.Find("notice", true).Single().Text.Contains("503"),
                "HTTP failure stays on input and enables retry");
        }
        preview.PerformClick();
        if (!live) handler.Complete(HttpStatusCode.OK);
        WaitFor(() => controller.ActivePage is HasilForm { Visible: true } && inputForm.IsDisposed);
        Check(controller.ActivePage is HasilForm, "Calculated result opens");
        var first = controller.Session.CurrentResult!;
        var results = controller.ActivePage!;
        var grid = (DataGridView)results.Controls.Find("dgvBulanan",true).Single();
        Check(grid.Rows.Count == 12 && grid.ReadOnly, "Result UI displays twelve read-only rows");
        var originalSize = window.Size;
        window.Size = new System.Drawing.Size(window.LogicalToDeviceUnits(960), window.LogicalToDeviceUnits(680));
        Application.DoEvents();
        var save = (Button)results.Controls.Find("btnSave", true).Single();
        Check(grid.DisplayedRowCount(false) == 12
            && window.RectangleToScreen(window.ClientRectangle).Contains(save.RectangleToScreen(save.ClientRectangle)),
            "Compact result fits all months and the save action");
        window.Size = originalSize;
        var firstTotal = first.Estimate.Summary!.TotalAnnualEnergyKwh;
        Check(live ? firstTotal > 0 : firstTotal == 2737.5, "Result uses calculated energy from NASA readings");
        Check(results.Controls.Find("lblTotalValue", true).Single().Text == firstTotal.ToString("N2", DisplayFormat.Culture)
            && results.Controls.Find("lblTotalUnit", true).Single().Text == "kWh",
            "Result shows the calculated total to two decimal places");
        int requestsBeforeComparison = handler.RequestCount;
        ((Button)results.Controls.Find("btnCompareCapacities", true).Single()).PerformClick();
        Check(controller.ActivePage is PerbandinganForm, "New result opens the comparison page");
        var fromNewResult = VerifyComparison(controller.ActivePage, firstTotal);
        Check(handler.RequestCount == requestsBeforeComparison && history.SaveRequests == 0,
            "Comparing an open result reuses its data without network or persistence calls");
        controller.Navigate(Screen.Result);
        results = controller.ActivePage;
        save = (Button)results.Controls.Find("btnSave", true).Single();
        var saveStatus = (Label)results.Controls.Find("lblStatus", true).Single();
        var failedSave = history.PauseNextSave();
        save.PerformClick();
        save.PerformClick();
        Check(history.SaveRequests == 1 && !save.Enabled && !rail.Enabled && saveStatus.Text.Contains("Menyimpan"),
            "Pending save shows progress and prevents a second request");
        failedSave.SetResult(false);
        WaitFor(() => save.Enabled);
        Check(first.Estimate.SimulationId == 0 && history.Count == 0 && saveStatus.Text == TestSimulationRepository.Error,
            "Failed save leaves the result unsaved and allows retry");
        save.PerformClick();
        WaitFor(() => save.Text == "Tersimpan");
        Check(!save.Enabled && history.Count == 1 && first.Estimate.SimulationId > 0, "Successful save records the database ID");
        ((Button)results.Controls.Find("btnEdit",true).Single()).PerformClick();
        Check(controller.ActivePage is Form1, "Result back returns to Input");
        inputForm = controller.ActivePage!;
        Check(inputForm.Controls.Find("tbLatitude", true).Single().Text == "-7,7956", "Coordinates survive returning to the input form");
        ((Button)inputForm.Controls.Find("btnLihatHasil", true).Single()).PerformClick();
        Check(controller.ActivePage is HasilForm && ReferenceEquals(controller.Session.CurrentResult, first)
            && !controller.ActivePage.Controls.Find("btnSave", true).Single().Enabled,
            "Unchanged input reopens the saved result without allowing a duplicate insert");
        controller.Navigate(Screen.Input);
        inputForm = controller.ActivePage!;
        ((NumericUpDown)inputForm.Controls.Find("nudKapasitas", true).Single()).Value = 4;
        int requestsBeforeRecalculation = handler.RequestCount;
        ((Button)inputForm.Controls.Find("btnLihatHasil", true).Single()).PerformClick();
        WaitFor(() => controller.ActivePage is HasilForm { Visible: true } && inputForm.IsDisposed);
        Check(live || handler.RequestCount == requestsBeforeRecalculation,
            "Recalculating the same coordinates reuses cached NASA readings");
        var second = controller.Session.CurrentResult!;
        Check(!ReferenceEquals(second, first) && Math.Abs(second.Estimate.Summary!.TotalAnnualEnergyKwh - 2 * firstTotal) <= 0.120001,
            "Doubling capacity approximately doubles the estimate");
        controller.Navigate(Screen.Home);
        var recent = (DataGridView)controller.ActivePage.Controls.Find("recentGrid", true).Single();
        WaitFor(() => recent.Rows.Count == 1);
        Check(recent.Rows[0].Cells[0].Value as string == first.Estimate.ScenarioName, "Home loads the saved simulation from the repository");

        history.FailNextList = true;
        controller.Navigate(Screen.History);
        var historyPage = controller.ActivePage;
        var historyGrid = (DataGridView)historyPage.Controls.Find("dgvHistory", true).Single();
        var historyStatus = (Label)historyPage.Controls.Find("lblStatus", true).Single();
        var refresh = (Button)historyPage.Controls.Find("btnRefresh", true).Single();
        var open = (Button)historyPage.Controls.Find("btnOpen", true).Single();
        WaitFor(() => historyStatus.Text == TestSimulationRepository.Error);
        Check(!historyGrid.Visible && !open.Enabled && refresh.Enabled, "Failed history load shows an error and offers retry");
        refresh.PerformClick();
        WaitFor(() => open.Enabled);
        Check(historyGrid.Rows.Count == 1 && historyGrid.Rows[0].Cells[2].Value as string == "2,00",
            "History loads the stored row and its original input after retry");

        history.FailNextOpen = true;
        open.PerformClick();
        Check(controller.ActivePage == historyPage && historyStatus.Text == TestSimulationRepository.Error && open.Enabled,
            "Failed detail load stays on History and allows retry");
        open.PerformClick();
        WaitFor(() => controller.ActivePage is HasilForm);
        var savedGrid = (DataGridView)controller.ActivePage.Controls.Find("dgvBulanan", true).Single();
        Check(savedGrid.Rows.Count == 12
            && savedGrid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells[1].Value as string).SequenceEqual(
                first.Estimate.Summary.MonthlyProductions.Select(month => month.EstimatedEnergyKwh.ToString("N2", DisplayFormat.Culture)))
            && controller.ActivePage.Controls.Find("lblTotalValue", true).Single().Text == firstTotal.ToString("N2", DisplayFormat.Culture),
            "Opening persisted detail reconstructs the monthly rows and annual total");
        Check(!controller.ActivePage!.Controls.Find("btnEdit",true).Single().Visible, "Saved detail hides edit action");
        ((Button)controller.ActivePage.Controls.Find("btnKembali",true).Single()).PerformClick();
        Check(controller.ActivePage is RiwayatForm, "Saved detail back returns to History");
        open = (Button)controller.ActivePage.Controls.Find("btnOpen", true).Single();
        WaitFor(() => open.Enabled);
        open.PerformClick();
        WaitFor(() => controller.ActivePage is HasilForm);
        requestsBeforeComparison = handler.RequestCount;
        ((Button)controller.ActivePage.Controls.Find("btnCompareCapacities", true).Single()).PerformClick();
        var fromHistory = VerifyComparison(controller.ActivePage, firstTotal);
        Check(fromHistory.SequenceEqual(fromNewResult) && handler.RequestCount == requestsBeforeComparison && history.SaveRequests == 2,
            "Reopened history yields identical comparison values without a new request or write");
        controller.Navigate(Screen.History);
        var delete = (Button)controller.ActivePage.Controls.Find("btnDelete", true).Single();
        WaitFor(() => delete.Enabled);
        RespondToDelete("btnCancel");
        Check(history.Count == 1 && history.DeleteRequests == 0, "Cancel keeps the selected history entry");
        history.FailNextDelete = true;
        RespondToDelete("btnConfirmDelete");
        Check(history.Count == 1 && delete.Enabled && controller.ActivePage.Controls.Find("lblStatus", true).Single().Text == TestSimulationRepository.Error,
            "Failed deletion keeps the row and allows retry");
        RespondToDelete("btnConfirmDelete");
        WaitFor(() => history.Count == 0 && !delete.Enabled);
        Check(history.Count == 0 && ((DataGridView)controller.ActivePage.Controls.Find("dgvHistory", true).Single()).Rows.Count == 0,
            "Successful deletion removes the row and disables row actions");
        controller.Navigate(Screen.Comparison);
        Check(controller.ActivePage.Controls.Find("lblComparisonDetails", true).Single().Text.Contains("Acuan 4 kWp"),
            "Deleting the viewed source falls back to the still-available simulation");
        controller.Navigate(Screen.Home);
        Application.DoEvents();
        Check(!controller.ActivePage.Controls.Find("recentGrid", true).Single().Visible,
            "Home no longer shows the deleted simulation");
        window.Close();
        Application.DoEvents();
        Check(Application.OpenForms.Count == 0, "Closing active form leaves no hidden windows");

        void RespondToDelete(string button)
        {
            window.BeginInvoke(() =>
            {
                var dialog = Application.OpenForms.Cast<Form>().Single(form => form.Text == "Hapus simulasi?");
                ((Button)dialog.Controls.Find(button, true).Single()).PerformClick();
            });
            delete.PerformClick();
        }
    }

    private static string[] VerifyComparison(Form comparison, double firstTotal)
    {
        var grid = (DataGridView)comparison.Controls.Find("dgvComparison", true).Single();
        var first = (TextBox)comparison.Controls.Find("tbAlternativeCapacity", true).Single();
        var second = (TextBox)comparison.Controls.Find("tbAdditionalCapacity", true).Single();
        var compare = (Button)comparison.Controls.Find("btnCompare", true).Single();
        var error = (Label)comparison.Controls.Find("lblValidation", true).Single();
        compare.PerformClick();
        Check(error.Visible && !grid.Visible, "Comparison requires an alternative capacity");
        first.Text = "2";
        compare.PerformClick();
        Check(error.Visible && error.Text.Contains("berbeda") && !grid.Visible, "Comparison rejects the original capacity as an alternative");
        first.Text = "4,00";
        compare.PerformClick();
        Check(grid.Visible && grid.Rows.Count == 12 && grid.Columns.Count == 3
            && comparison.Controls.Find("lblComparisonTotal0", true).Single().Text == firstTotal.ToString("N2", DisplayFormat.Culture),
            "Two-way comparison shows the original result and all twelve months");
        var values = grid.Rows.Cast<DataGridViewRow>().SelectMany(row => row.Cells.Cast<DataGridViewCell>())
            .Select(cell => cell.Value?.ToString() ?? "").ToArray();
        second.Text = "5.5";
        Check(!grid.Visible && grid.Rows.Count == 0, "Editing a capacity clears the previous comparison");
        compare.PerformClick();
        Check(grid.Visible && grid.Rows.Count == 12 && grid.Columns.Count == 4
            && grid.Columns[3].HeaderText == "5,5 kWp", "A third capacity accepts a decimal point and retains input order");
        return values;
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
