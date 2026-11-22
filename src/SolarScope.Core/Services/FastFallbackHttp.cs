using System.Net;
using System.Net.Sockets;

namespace SolarScope.Core.Services;

// SocketsHttpHandler tries resolved addresses one by one. On networks where IPv6 is advertised
// but stalls, the first connection waits for the IPv6 timeout (about 21 s on Windows) before
// falling back to IPv4, which exceeds the NASA and map timeouts. This connects like
// "Happy Eyeballs" (RFC 8305): start the next address after a short delay and keep the first
// socket that connects.
public static class FastFallbackHttp
{
    private static readonly TimeSpan AttemptDelay = TimeSpan.FromMilliseconds(250);

    public static HttpClient CreateClient() => new(new SocketsHttpHandler
    {
        ConnectCallback = ConnectAsync,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });

    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var endpoint = context.DnsEndPoint;
        var addresses = Interleave(await Dns.GetHostAddressesAsync(endpoint.Host, cancellationToken).ConfigureAwait(false));
        return await ConnectAsync(addresses, endpoint.Port, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Stream> ConnectAsync(IReadOnlyList<IPAddress> addresses, int port, CancellationToken cancellationToken)
    {
        if (addresses.Count == 0) throw new SocketException((int)SocketError.HostNotFound);

        using var attempts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pending = new List<Task<Socket>>();
        Exception? lastError = null;
        int next = 0;
        while (next < addresses.Count || pending.Count > 0)
        {
            if (next < addresses.Count) pending.Add(ConnectSocketAsync(addresses[next++], port, attempts.Token));
            var wait = next < addresses.Count
                ? Task.Delay(AttemptDelay, attempts.Token)
                : Task.Delay(Timeout.InfiniteTimeSpan, attempts.Token);
            await Task.WhenAny(pending.Append<Task>(wait)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var attempt in pending.Where(task => task.IsCompleted).ToList())
            {
                pending.Remove(attempt);
                if (attempt.IsCompletedSuccessfully)
                {
                    attempts.Cancel();
                    foreach (var loser in pending)
                    {
                        _ = loser.ContinueWith(task => { if (task.IsCompletedSuccessfully) task.Result.Dispose(); },
                            TaskScheduler.Default);
                    }
                    return new NetworkStream(attempt.Result, ownsSocket: true);
                }
                lastError = attempt.Exception?.InnerException ?? lastError;
            }
        }

        throw lastError ?? new SocketException((int)SocketError.HostUnreachable);
    }

    // Alternate address families, starting with the family the resolver returned first.
    private static List<IPAddress> Interleave(IPAddress[] addresses)
    {
        var first = addresses.Where(a => a.AddressFamily == addresses[0].AddressFamily).ToArray();
        var second = addresses.Where(a => a.AddressFamily != addresses[0].AddressFamily).ToArray();
        var result = new List<IPAddress>(addresses.Length);
        for (int i = 0; i < Math.Max(first.Length, second.Length); i++)
        {
            if (i < first.Length) result.Add(first[i]);
            if (i < second.Length) result.Add(second[i]);
        }
        return result;
    }

    private static async Task<Socket> ConnectSocketAsync(IPAddress address, int port, CancellationToken cancellationToken)
    {
        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(address, port, cancellationToken).ConfigureAwait(false);
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
