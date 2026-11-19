using static SolarScope.DesktopLayout;

namespace SolarScope;

internal sealed class DeleteSimulationDialog : Form
{
    public DeleteSimulationDialog(string name)
    {
        Text = "Hapus simulasi?";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = ShowInTaskbar = false;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(430, 156);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = DesktopLayout.Font(11);
        BackColor = Color.White;
        Padding = new Padding(24);
        var cancel = Button("btnCancel", "Batal");
        cancel.DialogResult = DialogResult.Cancel;
        var delete = Button("btnConfirmDelete", "Hapus", danger: true);
        delete.DialogResult = DialogResult.Yes;
        AcceptButton = CancelButton = cancel;
        var message = DesktopLayout.Text($"“{name}” akan dihapus dari riwayat sesi.");
        message.MaximumSize = new Size(382, 0);
        Controls.Add(Stack(14, DesktopLayout.Text("Hapus simulasi?", 15, true),
            message, Across(new Panel { Size = Size.Empty }, Actions(cancel, delete))));
        ActiveControl = cancel;
    }
}
