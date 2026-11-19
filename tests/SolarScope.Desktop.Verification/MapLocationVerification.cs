using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using Mapsui.UI.WindowsForms;
using Mapsui.Projections;
using SolarScope;
using Screen = SolarScope.Screen;

internal static class MapLocationVerification
{
    private const string KeyName = "SOLARSCOPE_GEOAPIFY_API_KEY";

    public static void Run()
    {
        var originalKey = Environment.GetEnvironmentVariable(KeyName);
        using var responses = new MapResponses();
        using var client = new HttpClient(responses);
        using var controller = new AppController(client, new TestSimulationRepository());
        var window = controller.MainForm!;
        Exception? failure = null;
        window.BeginInvoke(() =>
        {
            try { Verify(controller, responses); }
            catch (Exception error) { failure = error; }
            finally { window.Close(); }
        });
        try { Application.Run(controller); }
        finally { Environment.SetEnvironmentVariable(KeyName, originalKey); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(failure);
    }

    private static void Verify(AppController controller, MapResponses responses)
    {
        controller.Navigate(Screen.Input);
        var form = controller.ActivePage;
        var name = Find<TextBox>(form, "tbLokasi");
        var latitude = Find<TextBox>(form, "tbLatitude");
        var longitude = Find<TextBox>(form, "tbLongitude");
        var open = Find<Button>(form, "btnPickLocation");
        var notice = Find<Label>(form, "notice");
        Environment.SetEnvironmentVariable(KeyName, null);
        open.PerformClick();
        Check(notice.Text.Contains(KeyName) && name.Enabled && responses.TileRequests == 0,
            "Without a key, the map explains setup and manual input stays available");
        Environment.SetEnvironmentVariable(KeyName, "test-key");
        Interact(dialog =>
        {
            var map = Find<MapControl>(dialog, "locationMap");
            var canvas = map.Controls.Cast<Control>().Single();
            var status = Find<Label>(dialog, "mapStatus");
            var confirm = Find<Button>(dialog, "btnUseMapLocation");
            WaitFor(() => responses.TileRequests > 0 && status.Text.Length == 0);
            var viewport = map.Map.Navigator.Viewport;
            var (lon, lat) = SphericalMercator.ToLonLat(viewport.CenterX, viewport.CenterY);
            Check(!confirm.Enabled && Math.Abs(lat + 7.7956) < 0.001 && Math.Abs(lon - 110.3695) < 0.001
                && responses.FirstTilePath?.StartsWith("/v1/tile/osm-carto/", StringComparison.Ordinal) == true,
                "An empty draft loads Geoapify tiles around Yogyakarta without preselecting a point");
            Click(canvas, canvas.Width / 2, canvas.Height / 2);
            WaitFor(() => confirm.Enabled);
            Check(responses.Lookups == 0, "The first click enables confirmation without a lookup");
            Find<Button>(dialog, "btnCancelMap").PerformClick();
        });
        Check(latitude.Text == "" && longitude.Text == "", "Cancel on an empty draft leaves the fields blank");
        name.Text = "Nama awal";
        latitude.Text = "-7,7956";
        longitude.Text = "110,3695";
        var original = controller.Session.Draft;
        Interact(dialog =>
        {
            var confirm = Find<Button>(dialog, "btnUseMapLocation");
            WaitFor(() => confirm.Enabled);
            var map = Find<MapControl>(dialog, "locationMap");
            var canvas = map.Controls.Cast<Control>().Single();
            var coordinates = Find<Label>(dialog, "mapCoordinates");
            Check(coordinates.Text.Contains("-7,7956") && coordinates.Text.Contains("110,3695"),
                "The map opens with the existing point");
            var pinText = coordinates.Text;
            var originalView = map.Map.Navigator.Viewport;
            var x = canvas.Width / 2;
            var y = canvas.Height / 2;
            Mouse(canvas, "OnMouseDown", x, y);
            Mouse(canvas, "OnMouseMove", x + 40, y + 24);
            Mouse(canvas, "OnMouseUp", x + 40, y + 24);
            WaitFor(() => map.Map.Navigator.Viewport.CenterX != originalView.CenterX);
            var zoomBefore = map.Map.Navigator.Viewport.Resolution;
            Find<Button>(dialog, "btnMapZoomIn").PerformClick();
            WaitFor(() => map.Map.Navigator.Viewport.Resolution < zoomBefore);
            Check(coordinates.Text == pinText && responses.Lookups == 0, "Dragging and zooming leave the selected pin unchanged");
            Click(canvas, x - 37, y);
            WaitFor(() => coordinates.Text != pinText);
            Check(responses.Lookups == 0, "Picking a point does not call reverse geocoding");
            Find<Button>(dialog, "btnCancelMap").PerformClick();
        });
        Check(controller.Session.Draft == original, "Cancel leaves the simulation draft unchanged");

        responses.FailTiles = true;
        Interact(dialog =>
        {
            var retry = Find<Button>(dialog, "btnRetryMap");
            WaitFor(() => retry.Visible);
            Check(!Find<Button>(dialog, "btnUseMapLocation").Enabled, "Tile failure offers retry and blocks confirmation");
            responses.FailTiles = false;
            retry.PerformClick();
            WaitFor(() => Find<Button>(dialog, "btnUseMapLocation").Enabled);
            Find<Button>(dialog, "btnUseMapLocation").PerformClick();
        });
        Check(name.Text == "Depok, Sleman" && latitude.Text == "-7,7956" && longitude.Text == "110,3695" && responses.Lookups == 1,
            "A successful retry and confirmation use the resolved name and the original pin coordinates");

        responses.FailLookup = true;
        Interact(dialog =>
        {
            var confirm = Find<Button>(dialog, "btnUseMapLocation");
            WaitFor(() => confirm.Enabled);
            var map = Find<MapControl>(dialog, "locationMap");
            var canvas = map.Controls.Cast<Control>().Single();
            var coordinates = Find<Label>(dialog, "mapCoordinates");
            var oldPoint = coordinates.Text;
            Click(canvas, canvas.Width / 2 + 30, canvas.Height / 2 - 16);
            WaitFor(() => coordinates.Text != oldPoint);
            confirm.PerformClick();
        });
        Check(name.Text == "" && (latitude.Text != "-7,7956" || longitude.Text != "110,3695")
            && notice.Text.Contains("Isi nama lokasi"), "A failed lookup applies the new pin and asks for a manual name");
        var pinnedDraft = controller.Session.Draft;
        responses.FailLookup = false;
        responses.DelayLookup = true;
        Interact(dialog =>
        {
            var confirm = Find<Button>(dialog, "btnUseMapLocation");
            WaitFor(() => confirm.Enabled);
            confirm.PerformClick();
            WaitFor(() => responses.Lookups == 3);
            Check(!confirm.Enabled, "A pending lookup prevents a second confirmation");
            Find<Button>(dialog, "btnCancelMap").PerformClick();
        });
        WaitFor(() => responses.LookupCanceled);
        Check(controller.Session.Draft == pinnedDraft, "Cancel during a pending lookup cancels the request and preserves the draft");

        Find<TextBox>(form, "tbNamaSimulasi").Text = "Uji lokasi peta";
        name.Text = "Nama manual";
        Find<Button>(form, "btnLihatHasil").PerformClick();
        WaitFor(() => controller.ActivePage is HasilForm);
        var result = controller.Session.CurrentResult!.Estimate;
        Check(result.Location.LocationName == "Nama manual" && responses.SolarQuery is { } query
            && query.Contains("latitude=" + double.Parse(pinnedDraft.Latitude, CultureInfo.GetCultureInfo("id-ID")).ToString(CultureInfo.InvariantCulture))
            && query.Contains("longitude=" + double.Parse(pinnedDraft.Longitude, CultureInfo.GetCultureInfo("id-ID")).ToString(CultureInfo.InvariantCulture))
            && result.Summary?.MonthlyProductions.Count == 12,
            "The normal estimation flow sends the chosen point to NASA POWER and displays 12 months");

        void Interact(Action<Form> interact)
        {
            Exception? failure = null;
            controller.MainForm!.BeginInvoke(() =>
            {
                var dialog = Application.OpenForms.Cast<Form>().Single(item => item.Text == "Pilih lokasi");
                try { interact(dialog); }
                catch (Exception error)
                {
                    failure = error;
                    dialog.Close();
                }
            });
            open.PerformClick();
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(failure);
        }
    }

    private static T Find<T>(Control parent, string name) where T : Control =>
        (T)parent.Controls.Find(name, true).Single();

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        Console.WriteLine("PASS " + message);
    }

    private static void WaitFor(Func<bool> test)
    {
        var elapsed = Stopwatch.StartNew();
        while (!test() && elapsed.Elapsed < TimeSpan.FromSeconds(10))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        if (!test()) throw new TimeoutException("The map did not reach the expected state.");
    }

    private static void Click(Control control, int x, int y)
    {
        Mouse(control, "OnMouseDown", x, y);
        Mouse(control, "OnMouseUp", x, y);
    }

    private static void Mouse(Control control, string method, int x, int y) =>
        typeof(Control).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, [new MouseEventArgs(MouseButtons.Left, 1, x, y, 0)]);

    private sealed class MapResponses : HttpMessageHandler
    {
        private readonly byte[] tile = MakeTile();
        private int tileRequests;
        public int TileRequests => tileRequests;
        public string? FirstTilePath { get; private set; }
        public volatile bool FailTiles;
        public bool FailLookup { get; set; }
        public bool DelayLookup { get; set; }
        public volatile bool LookupCanceled;
        public int Lookups { get; private set; }
        public string? SolarQuery { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var uri = request.RequestUri!;
            if (uri.Host == "maps.geoapify.com")
            {
                if (Interlocked.Increment(ref tileRequests) == 1) FirstTilePath = uri.AbsolutePath;
                if (FailTiles) return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                var content = new ByteArrayContent(tile);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            }
            if (uri.Host == "api.geoapify.com")
            {
                Lookups++;
                if (DelayLookup)
                {
                    try { await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false); }
                    finally { LookupCanceled = token.IsCancellationRequested; }
                }
                return Json(FailLookup
                    ? """{"results":[]}"""
                    : """{"results":[{"district":"Depok","county":"Sleman","lat":0,"lon":0}]}""");
            }
            SolarQuery = uri.Query;
            return Json("""
                {"properties":{"parameter":{"ALLSKY_SFC_SW_DWN":{
                    "JAN":5,"FEB":5,"MAR":5,"APR":5,"MAY":5,"JUN":5,
                    "JUL":5,"AUG":5,"SEP":5,"OCT":5,"NOV":5,"DEC":5
                }}},"parameters":{"ALLSKY_SFC_SW_DWN":{"units":"kW-hr/m^2/day"}}}
                """);
        }

        private static HttpResponseMessage Json(string body) =>
            new(HttpStatusCode.OK) { Content = new StringContent(body) };

        private static byte[] MakeTile()
        {
            using var bitmap = new Bitmap(256, 256);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Beige);
            graphics.DrawLine(Pens.Goldenrod, 0, 0, 256, 256);
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }
}
