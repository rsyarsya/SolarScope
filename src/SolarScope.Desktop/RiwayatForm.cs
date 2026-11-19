namespace SolarScope;

public partial class RiwayatForm : Form
{
    private AppController? controller;

    public RiwayatForm() => InitializeComponent();

    public RiwayatForm(AppController controller) : this()
    {
        this.controller = controller;
        btnOpen.Click += (_, _) => OpenSelected();
        dgvHistory.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenSelected(); };
        dgvHistory.SelectionChanged += (_, _) => UpdateActions();
        btnDelete.Click += (_, _) => DeleteSelected();
        btnCreate.Click += (_, _) => controller.Navigate(Screen.Input);
        RefreshHistory();
    }

    private HistoryEntry? Selected() =>
        dgvHistory.SelectedRows.Count == 1 ? dgvHistory.SelectedRows[0].Tag as HistoryEntry : null;

    private void OpenSelected()
    {
        if (Selected() is { } entry) controller?.ShowResult(entry.Result, Screen.History);
    }

    private void UpdateActions() => btnOpen.Enabled = btnDelete.Enabled = Selected() is not null;

    private void RefreshHistory()
    {
        dgvHistory.Rows.Clear();
        foreach (var entry in controller!.Session.History)
        {
            var input = entry.Result.Input;
            int row = dgvHistory.Rows.Add(input.Name, input.Location, input.Capacity.ToString("N2", DemoData.Culture),
                entry.SavedAt.ToString("dd MMM yyyy, HH.mm", DemoData.Culture));
            dgvHistory.Rows[row].Tag = entry;
        }
        DesktopLayout.SizeGrid(dgvHistory, 10);
        bool hasRows = dgvHistory.Rows.Count > 0;
        emptyPanel.Visible = !hasRows;
        if (hasRows) dgvHistory.CurrentCell = dgvHistory.Rows[0].Cells[0];
        UpdateActions();
    }

    private void DeleteSelected()
    {
        if (Selected() is not { } entry) return;
        using var dialog = new DeleteSimulationDialog(entry.Result.Input.Name);
        if (dialog.ShowDialog(controller!.MainForm) != DialogResult.Yes) return;
        controller.Session.Delete(entry.Result.Id);
        RefreshHistory();
        lblStatus.Text = "Simulasi dihapus dari riwayat sesi.";
        lblStatus.Visible = true;
    }
}
