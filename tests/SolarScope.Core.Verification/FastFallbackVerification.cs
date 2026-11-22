using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using SolarScope.Core.Services;

internal static class FastFallbackVerification
{
    public static async Task VerifyAsync(Action<bool, string> check)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var accept = listener.AcceptTcpClientAsync();

        // 10.255.255.1 is a non-routable address: a SYN sent there normally gets no answer.
        var stalled = IPAddress.Parse("10.255.255.1");
        var timer = Stopwatch.StartNew();
        await using (var stream = await FastFallbackHttp.ConnectAsync([stalled, IPAddress.Loopback], port, CancellationToken.None))
        {
            timer.Stop();
            using var server = await accept;
            check(stream.CanWrite && timer.Elapsed < TimeSpan.FromSeconds(3),
                $"Stalled first address falls back to the next address quickly ({timer.ElapsedMilliseconds} ms)");
        }

        using var canceled = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        try
        {
            await FastFallbackHttp.ConnectAsync([stalled], port, canceled.Token);
            check(false, "Caller cancellation stops pending connection attempts");
        }
        catch (OperationCanceledException)
        {
            check(true, "Caller cancellation stops pending connection attempts");
        }

        listener.Stop();
        try
        {
            await FastFallbackHttp.ConnectAsync([IPAddress.Loopback], port, CancellationToken.None);
            check(false, "Unreachable addresses surface a socket error");
        }
        catch (SocketException)
        {
            check(true, "Unreachable addresses surface a socket error");
        }
    }
}
