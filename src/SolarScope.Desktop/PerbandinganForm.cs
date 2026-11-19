using static SolarScope.DesktopLayout;

namespace SolarScope;

public partial class PerbandinganForm : Form
{
    public PerbandinganForm() => InitializeComponent();

    public PerbandinganForm(AppController controller) : this()
    {
        CheckBox[] choices = [chk2, chk3, chk5];
        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            var choice = choices[i];
            choice.Checked = controller.Session.ComparisonSelection[i];
            choice.CheckedChanged += (_, _) =>
            {
                controller.Session.ComparisonSelection[index] = choice.Checked;
                ResetTable();
                lblSummary.Text = "Pilihan berubah. Pilih Bandingkan untuk memperbarui tabel.";
                lblSummary.Visible = true;
            };
        }
        btnCompare.Click += (_, _) => Compare(choices);
        Compare(choices);
    }

    private void ResetTable()
    {
        dgvComparison.Columns.Clear();
        dgvComparison.Visible = false;
        totals.Visible = false;
        foreach (var control in totals.Controls.Cast<Control>().ToArray()) control.Dispose();
        totals.ColumnStyles.Clear();
        lblValidation.Visible = lblSummary.Visible = false;
    }

    private void Compare(CheckBox[] choices)
    {
        ResetTable();
        var selected = DemoData.Scenarios.Where((_, i) => choices[i].Checked).ToArray();
        if (!DemoData.ValidComparison(selected.Length))
        {
            lblValidation.Text = "Pilih minimal dua kapasitas untuk dibandingkan.";
            lblValidation.Visible = true;
            return;
        }
        Column(dgvComparison, "month", "Bulan");
        totals.ColumnCount = selected.Length + 1;
        for (int i = 0; i < totals.ColumnCount; i++)
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / totals.ColumnCount));
        var caption = Stack(2, DesktopLayout.Text("Total tahunan", 10.5f, true, DarkGreen), Note("kWh"));
        caption.Padding = new Padding(12, 8, 0, 8);
        totals.Controls.Add(caption, 0, 0);
        for (int i = 0; i < selected.Length; i++)
        {
            var scenario = selected[i];
            Column(dgvComparison, $"scenario{i}", $"{scenario.Capacity} kWp (kWh)", numeric: true);
            var value = DesktopLayout.Text(DemoData.Number(scenario.AnnualTotal), 22, true, SolarTheme.Green);
            value.Anchor = AnchorStyles.Right;
            value.Margin = new Padding(0, 0, 13, 0);
            totals.Controls.Add(value, i + 1, 0);
        }
        for (int month = 0; month < 12; month++)
        {
            var cells = new List<object> { DemoData.Month(month + 1) };
            cells.AddRange(selected.Select(scenario => (object)DemoData.Number(scenario.Months[month].Energy)));
            dgvComparison.Rows.Add(cells.ToArray());
        }
        SizeGrid(dgvComparison);
        totals.Visible = dgvComparison.Visible = true;
    }
}
