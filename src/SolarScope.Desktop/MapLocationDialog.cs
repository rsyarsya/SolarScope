using System.Diagnostics;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Manipulations;
using Mapsui.Projections;
using Mapsui.Tiling.Layers;
using Mapsui.UI.WindowsForms;
using SolarScope.Core.Services;
using SymbolStyle = Mapsui.Styles.SymbolStyle;
using SymbolType = Mapsui.Styles.SymbolType;
using static SolarScope.DesktopLayout;

namespace SolarScope;

internal readonly record struct MapPoint(double Latitude, double Longitude)
{
    private const double MercatorLimit = 85.05112878;

    public bool CanPlot => double.IsFinite(Latitude) && Math.Abs(Latitude) <= MercatorLimit
        && double.IsFinite(Longitude) && Math.Abs(Longitude) <= 180;

    public MapPoint Rounded() => new(Math.Round(Latitude, 6), Math.Round(Longitude, 6));

    public MPoint ToWorld() => SphericalMercator.FromLonLat(new MPoint(Longitude, Latitude));
}

internal sealed record MapSelection(MapPoint Point, string? Region);

internal sealed class MapLocationDialog : Form
{
    private const string Tiles = "https://maps.geoapify.com/v1/tile/osm-carto";
    private const double ZoomZeroResolution = 156543.03392804097;
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly SemaphoreSlim TileRequests = new(1);
    private static long nextTileRequest;
    private readonly MapControl map = new() { Name = "locationMap", Dock = DockStyle.Fill, AccessibleName = "Peta lokasi" };
    private readonly MemoryLayer marker = new()
    {
        Name = "Titik terpilih",
        Style = new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            SymbolScale = 0.55,
            Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 252, 205, 42)),
            Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.FromArgb(255, 35, 76, 28), 2)
        }
    };
    private readonly Label status = Note("");
    private readonly Label coordinates = Note("Klik peta untuk memilih titik.");
    private readonly Button retry = Button("btnRetryMap", "Muat ulang peta");
    private readonly Button confirm = Button("btnUseMapLocation", "Gunakan lokasi ini", primary: true);
    private readonly CancellationTokenSource lifetime = new();
    private readonly HttpClient httpClient;
    private readonly string escapedKey;
    private readonly GeoapifyReverseGeocodingClient geocoder;
    private TileLayer? tiles;
    private TileLoad? currentLoad;
    private MapPoint? point;
    private bool mapReady;
    private bool resolving;

    public MapSelection? Selection { get; private set; }

    public MapLocationDialog(HttpClient httpClient, string apiKey, MapPoint? initial)
    {
        this.httpClient = httpClient;
        escapedKey = Uri.EscapeDataString(apiKey);
        geocoder = new GeoapifyReverseGeocodingClient(httpClient, apiKey);

        Text = "Pilih lokasi";
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(738, 526);
        MinimumSize = new Size(570, 425);
        Padding = new Padding(16);
        BackColor = Canvas;
        Font = DesktopLayout.Font(11);

        status.Name = "mapStatus";
        status.AccessibleRole = AccessibleRole.Alert;
        coordinates.Name = "mapCoordinates";
        retry.Visible = false;
        confirm.Enabled = false;
        var cancel = Button("btnCancelMap", "Batal");
        cancel.DialogResult = DialogResult.Cancel;
        CancelButton = cancel;
        AcceptButton = confirm;
        var zoomOut = Button("btnMapZoomOut", "−");
        zoomOut.AccessibleName = "Perkecil peta";
        var zoomIn = Button("btnMapZoomIn", "+");
        zoomIn.AccessibleName = "Perbesar peta";
        zoomOut.Click += (_, _) => map.Map.Navigator.ZoomOut();
        zoomIn.Click += (_, _) => map.Map.Navigator.ZoomIn();
        var attribution = Actions(Attribution("Geoapify", "https://www.geoapify.com/"),
            Attribution("© OpenStreetMap contributors", "https://www.openstreetmap.org/copyright"));
        var content = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, Margin = Padding.Empty };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(Across(coordinates, Actions(zoomOut, zoomIn)), 0, 0);
        content.Controls.Add(map, 0, 1);
        content.Controls.Add(Stack(8, Across(attribution, retry), status, Across(Note(""), Actions(cancel, confirm))), 0, 2);
        Controls.Add(content);
        confirm.Click += async (_, _) => await ConfirmAsync();
        retry.Click += (_, _) => LoadTiles();
        map.Map.Tapped += (_, e) =>
        {
            if (e.GestureType != GestureType.SingleTap || !mapReady || resolving) return;
            var (longitude, latitude) = SphericalMercator.ToLonLat(e.WorldPosition.X, e.WorldPosition.Y);
            var selected = new MapPoint(latitude, longitude);
            if (selected.CanPlot) SetPoint(selected);
        };
        map.Map.Layers.Add(marker);
        if (initial is { CanPlot: true } existing) SetPoint(existing);
        Shown += (_, _) =>
        {
            var center = point ?? new MapPoint(-7.7956, 110.3695);
            var zoom = point is null ? 11 : 14;
            map.Map.Navigator.CenterOnAndZoomTo(center.ToWorld(), ZoomZeroResolution / (1 << zoom));
            LoadTiles();
        };
    }

    private void SetPoint(MapPoint selected)
    {
        point = selected;
        var rounded = selected.Rounded();
        marker.Features = [new PointFeature(selected.ToWorld())];
        coordinates.Text = $"Lat. {Form1.FormatCoordinate(rounded.Latitude)}  ·  Lon. {Form1.FormatCoordinate(rounded.Longitude)}";
        confirm.Enabled = mapReady && !resolving;
        map.RefreshGraphics();
    }

    private void LoadTiles()
    {
        var load = new TileLoad();
        currentLoad?.Dispose();
        currentLoad = load;
        if (tiles is not null) map.Map.Layers.Remove(tiles);
        mapReady = false;
        confirm.Enabled = retry.Visible = false;
        status.Text = "Memuat peta…";
        var schema = new GlobalSphericalMercator();
        var source = new HttpTileSource(schema, Tiles + "/{z}/{x}/{y}.png", name: "Geoapify");
        tiles = new TileLayer(source, fetchTileAsFeature: tile => FetchTileAsync(load, tile));
        map.Map.Layers.Insert(0, tiles);
        map.RefreshData();
    }

    private async Task<IFeature?> FetchTileAsync(TileLoad load, BruTile.TileInfo tile)
    {
        try
        {
            await WaitForTileSlotAsync(load.Token).ConfigureAwait(false);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(load.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            var index = tile.Index;
            var url = $"{Tiles}/{index.Level}/{index.Col}/{index.Row}.png?apiKey={escapedKey}";
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType != "image/png")
            {
                ReportTile(load, ready: false);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(false);
            if (bytes.AsSpan().StartsWith(PngSignature))
            {
                ReportTile(load, ready: true);
                var extent = tile.Extent;
                return new RasterFeature(new MRaster(bytes, new MRect(extent.MinX, extent.MinY, extent.MaxX, extent.MaxY)));
            }
        }
        catch (Exception error) when (error is HttpRequestException or IOException or OperationCanceledException)
        {
            // Report a generic tile failure without forwarding request URIs or credentials.
        }
        if (!load.Token.IsCancellationRequested) ReportTile(load, ready: false);
        return null;
    }

    private static async Task WaitForTileSlotAsync(CancellationToken token)
    {
        await TileRequests.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var remaining = Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), nextTileRequest);
            if (remaining > TimeSpan.Zero) await Task.Delay(remaining, token).ConfigureAwait(false);
            nextTileRequest = Stopwatch.GetTimestamp() + (Stopwatch.Frequency + 3) / 4;
        }
        finally
        {
            TileRequests.Release();
        }
    }

    private void ReportTile(TileLoad load, bool ready)
    {
        if (load.Token.IsCancellationRequested || (ready ? !load.FirstSuccess() : !load.FirstFailure())) return;
        try
        {
            BeginInvoke(() =>
            {
                if (!ReferenceEquals(load, currentLoad) || IsDisposed || load.Token.IsCancellationRequested) return;
                if (ready)
                {
                    mapReady = true;
                    confirm.Enabled = !resolving && point is not null;
                }
                else retry.Visible = !resolving;

                if (!resolving)
                    status.Text = retry.Visible ? (mapReady ? "Sebagian peta gagal dimuat." : "Peta gagal dimuat.") : "";
            });
        }
        catch (InvalidOperationException) when (load.Token.IsCancellationRequested || Disposing || IsDisposed) { }
    }

    private async Task ConfirmAsync()
    {
        if (resolving || !mapReady || point is not { } selected) return;
        resolving = true;
        confirm.Enabled = retry.Enabled = map.Enabled = false;
        status.Text = "Mencari nama wilayah…";
        var token = lifetime.Token;
        var rounded = selected.Rounded();
        try
        {
            var result = await geocoder.GetRegionAsync(rounded.Latitude, rounded.Longitude, token);
            if (token.IsCancellationRequested || IsDisposed) return;
            Selection = new MapSelection(rounded, result.Data);
            DialogResult = DialogResult.OK;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        lifetime.Cancel();
        currentLoad?.Dispose();
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            lifetime.Cancel();
            currentLoad?.Dispose();
            lifetime.Dispose();
        }
        base.Dispose(disposing);
    }

    private static LinkLabel Attribution(string text, string url)
    {
        var link = new LinkLabel { Text = text, AutoSize = true, Font = DesktopLayout.Font(9), LinkColor = DarkGreen };
        link.LinkClicked += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose(); }
            catch (System.ComponentModel.Win32Exception) { }
        };
        return link;
    }

    private sealed class TileLoad : IDisposable
    {
        private readonly CancellationTokenSource cancellation = new();
        private int success;
        private int failure;
        public CancellationToken Token { get; }

        public TileLoad() => Token = cancellation.Token;

        public bool FirstSuccess() => Interlocked.Exchange(ref success, 1) == 0;
        public bool FirstFailure() => Interlocked.Exchange(ref failure, 1) == 0;

        public void Dispose()
        {
            if (Token.IsCancellationRequested) return;
            cancellation.Cancel();
            cancellation.Dispose();
        }
    }
}
