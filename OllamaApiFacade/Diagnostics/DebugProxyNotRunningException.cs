using System.Net.Sockets;

namespace OllamaApiFacade.Diagnostics;

public sealed class DebugProxyNotRunningException : Exception
{
    public DebugProxyNotRunningException(string proxyUrl, Exception inner)
        : base($"The configured debug proxy is not running at {proxyUrl}. Start Burp Suite or adjust the proxy URL.", inner)
        => ProxyUrl = proxyUrl;

    public string ProxyUrl { get; }
}

public static class ProxyDiagnostics
{
    public static void ThrowIfProxyDown(Exception ex)
    {
        var proxyUrl = GetProxyUrl();
        if (proxyUrl is null) return;

        if (IsConnectionRefused(ex))
            throw new DebugProxyNotRunningException(proxyUrl, ex);
    }

    public static bool TryPingProxy(string proxyUrl, int timeoutMs = 500)
    {
        if (!Uri.TryCreate(proxyUrl, UriKind.Absolute, out var uri)) return false;
        using var cts = new CancellationTokenSource(timeoutMs);
        using var client = new TcpClient();
        try
        {
            var task = client.ConnectAsync(uri.Host, uri.Port);
            task.Wait(cts.Token);
            return client.Connected;
        }
        catch { return false; }
    }

    public static string? GetProxyUrl()
        => Environment.GetEnvironmentVariable("HTTPS_PROXY")
        ?? Environment.GetEnvironmentVariable("HTTP_PROXY")
        ?? Environment.GetEnvironmentVariable("ALL_PROXY");

    static bool IsConnectionRefused(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is SocketException se && se.SocketErrorCode == SocketError.ConnectionRefused) return true;
            var msg = e.Message;
            if (msg.Contains("connection refused", StringComparison.OrdinalIgnoreCase)) return true;
            if (msg.Contains("Verbindung verweigerte", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
