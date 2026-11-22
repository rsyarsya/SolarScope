using static SolarScope.DesktopLayout;

namespace SolarScope;

partial class PanduanForm
{

    private void InitializeComponent()
    {
        var latitude = Coordinate("Latitude", "−7,7956");
        var longitude = Coordinate("Longitude", "110,3695");
        var examples = Actions(DesktopLayout.Text("Contoh Yogyakarta", 10.5f, true), latitude, longitude);
        foreach (Control child in examples.Controls) child.Anchor = AnchorStyles.None;
        var content = Stack(18,
            Stack(8, DesktopLayout.Text("Mengisi koordinat", 15, true),
                Wrap("Cari lokasi di aplikasi peta, lalu salin latitude dan longitude dalam derajat desimal. Masukkan latitude terlebih dahulu."),
                examples, Wrap("Latitude berkisar dari −90 hingga 90. Longitude berkisar dari −180 hingga 180.", 9.5f)),
            Rule(),
            Stack(8, DesktopLayout.Text("Arti kWp dan kWh", 15, true),
                Wrap("kWp menunjukkan daya puncak panel. Enam panel 500 Wp memiliki kapasitas total 3 kWp."),
                Wrap("kWh menunjukkan energi dalam suatu periode. Misalnya, 340 kWh pada Januari adalah perkiraan energi selama bulan itu.")),
            Rule(),
            Stack(8, DesktopLayout.Text("Mengapa produksi aktual bisa berbeda?", 15, true),
                Wrap("NASA POWER memberi rata-rata radiasi matahari jangka panjang. Bayangan, arah panel, suhu, dan kondisi instalasi dapat memengaruhi produksi aktual.")));
        Page(this, "Panduan", Stack(12, Title("Panduan"), Card(content, 744)));
    }
}
