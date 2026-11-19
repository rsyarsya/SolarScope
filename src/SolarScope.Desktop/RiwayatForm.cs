using SolarScope.Core.Models;

namespace SolarScope;

public partial class RiwayatForm : Form
{
    private bool busy;

    public RiwayatForm() => InitializeComponent();

    public RiwayatForm(AppController controller) : this()
    {
        btnOpen.Click += async (_, _) => await OpenSelectedAsync(controller);
        dgvHistory.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0) await OpenSelectedAsync(controller);
        };
        dgvHistory.SelectionChanged += (_, _) => UpdateActions();
        btnDelete.Click += async (_, _) => await DeleteSelectedAsync(controller);
        btnRefresh.Click += async (_, _) => await RefreshHistoryAsync(controller);
        btnCreate.Click += (_, _) => controller.Navigate(Screen.Input);
        Shown += async (_, _) => await RefreshHistoryAsync(controller);
        UpdateActions();
    }

    private Simulation? Selected() =>
        dgvHistory.SelectedRows.Count == 1 ? dgvHistory.SelectedRows[0].Tag as Simulation : null;

    private async Task OpenSelectedAsync(AppController controller)
    {
        if (busy || Selected() is not { } selected) return;
        StartOperation("Membuka simulasi…");
        var detail = await controller.History.GetSimulationByIdAsync(selected.SimulationId);
        if (IsDisposed) return;
        FinishOperation(detail.ErrorMessage);
        if (detail.IsSuccess) controller.ShowResult(new SimulationResult(null, detail.Data!), Screen.History);
    }

    private void UpdateActions()
    {
        btnOpen.Enabled = btnDelete.Enabled = !busy && Selected() is not null;
        btnRefresh.Enabled = !busy;
    }

    private async Task RefreshHistoryAsync(AppController controller)
    {
        if (busy) return;
        StartOperation("Memuat riwayat…");
        var history = await controller.History.GetAllSimulationsAsync();
        if (IsDisposed) return;
        if (!history.IsSuccess)
        {
            FinishOperation(history.ErrorMessage);
            return;
        }

        dgvHistory.Rows.Clear();
        foreach (var entry in history.Data!)
        {
            int row = dgvHistory.Rows.Add(entry.ScenarioName, entry.Location.LocationName,
                entry.SystemParameter.SystemCapacityKwp.ToString("N2", DisplayFormat.Culture),
                entry.CreatedAt.ToLocalTime().ToString("dd MMM yyyy, HH.mm", DisplayFormat.Culture));
            dgvHistory.Rows[row].Tag = entry;
        }
        UpdateRows();
        FinishOperation(null);
    }

    private void UpdateRows()
    {
        bool hasRows = dgvHistory.Rows.Count > 0;
        dgvHistory.Visible = hasRows;
        emptyPanel.Visible = !hasRows;
        if (hasRows)
        {
            DesktopLayout.SizeGrid(dgvHistory, 10);
            if (dgvHistory.CurrentCell is null) dgvHistory.CurrentCell = dgvHistory.Rows[0].Cells[0];
        }
    }

    private async Task DeleteSelectedAsync(AppController controller)
    {
        if (busy || Selected() is not { } selected) return;
        using var dialog = new DeleteSimulationDialog(selected.ScenarioName);
        if (dialog.ShowDialog(controller.MainForm) != DialogResult.Yes) return;

        var row = dgvHistory.SelectedRows[0];
        StartOperation("Menghapus simulasi…");
        var deletion = await controller.WriteHistoryAsync(() => controller.History.DeleteSimulationAsync(selected.SimulationId));
        if (IsDisposed) return;
        if (deletion.IsSuccess)
        {
            controller.ForgetDeletedSimulation(selected.SimulationId);
            dgvHistory.Rows.Remove(row);
            UpdateRows();
        }
        FinishOperation(deletion.IsSuccess ? "Simulasi dihapus." : deletion.ErrorMessage);
    }

    private void StartOperation(string message)
    {
        busy = true;
        lblStatus.Text = message;
        lblStatus.Visible = true;
        UpdateActions();
    }

    private void FinishOperation(string? message)
    {
        busy = false;
        lblStatus.Text = message;
        lblStatus.Visible = !string.IsNullOrEmpty(message);
        UpdateActions();
    }
}
