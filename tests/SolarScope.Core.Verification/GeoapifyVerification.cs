using System.Globalization;
using System.Net;
using SolarScope.Core.Services;

internal static class GeoapifyVerification
{
    public static async Task VerifyAsync(Action<bool, string> check)
    {
        Uri? requestUri = null;
        using var ok = new HttpClient(new Stub((request, _) =>
        {
            requestUri = request.RequestUri;
            return Reply("""{"results":[{"district":"Depok","county":"Sleman","lat":90,"lon":0}]}""");
        }));
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("id-ID");
            var result = await new GeoapifyReverseGeocodingClient(ok, "test+key").GetRegionAsync(-7.7956, 110.3695);
            check(result.IsSuccess && result.Data == "Depok, Sleman", "Reverse geocoding uses administrative names");
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
        check(requestUri?.AbsoluteUri == "https://api.geoapify.com/v1/geocode/reverse?lat=-7.7956&lon=110.3695&format=json&lang=id&limit=1&apiKey=test%2Bkey",
            "Reverse geocoding sends invariant coordinates and an encoded key");

        foreach (var (payload, expected) in new (string, string?)[]
        {
            ("""{"results":[{"district":" Sleman ","city":"sleman"}]}""", "Sleman"),
            ("""{"results":[{"suburb":" ","state":"DI Yogyakarta","country":"Indonesia"}]}""", "DI Yogyakarta"),
            ("""{"results":[]}""", null),
            ("""{"results":[{"street":"Jalan Kaliurang"}]}""", null),
            ("{", null)
        })
        {
            using var client = new HttpClient(new Stub((_, _) => Reply(payload)));
            var result = await new GeoapifyReverseGeocodingClient(client, "test").GetRegionAsync(-7, 110);
            check(result.IsSuccess == (expected is not null) && result.Data == expected, $"Reverse geocoding handles {expected ?? "an unresolved name"}");
        }

        using var failed = new HttpClient(new Stub((_, _) => Reply("", HttpStatusCode.TooManyRequests)));
        var rejected = await new GeoapifyReverseGeocodingClient(failed, "test").GetRegionAsync(-7, 110);
        check(!rejected.IsSuccess, "Reverse geocoding handles HTTP failures");

        using var slow = new HttpClient(new Stub(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        })) { Timeout = TimeSpan.FromMilliseconds(100) };
        var timedOut = await new GeoapifyReverseGeocodingClient(slow, "test").GetRegionAsync(-7, 110);
        check(!timedOut.IsSuccess, "Reverse geocoding handles timeouts");

        using var caller = new CancellationTokenSource();
        using var canceled = new HttpClient(new Stub((_, token) =>
        {
            caller.Cancel();
            token.ThrowIfCancellationRequested();
            return Reply("");
        }));
        try
        {
            await new GeoapifyReverseGeocodingClient(canceled, "test").GetRegionAsync(-7, 110, caller.Token);
            check(false, "Reverse geocoding propagates caller cancellation");
        }
        catch (OperationCanceledException) when (caller.IsCancellationRequested)
        {
            check(true, "Reverse geocoding propagates caller cancellation");
        }
    }

    private static Task<HttpResponseMessage> Reply(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });

    private sealed class Stub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
