namespace OllamaApiFacade.Diagnostics;

public sealed class ProxyHttp2IncompatibilityException(string message, Exception inner) : Exception(message, inner);

public static class ProxyHttp2Diagnostics
{
    public static void ThrowIfLikelyBurpHttp2(Exception exception)
    {
        if (!IsProxyEnabled()) return;
        if (!LooksLikeHttp2StatusLineIssue(exception)) return;

        var proxyUrl = GetProxyEnv() ?? "<unknown>";

        var msg =
            $@"Proxy detected: {proxyUrl}
This looks like an HTTP2 issue via a debug proxy.
Typical error: 'Received an invalid status line: HTTP/2 200 OK'.

Solution in Burp Suite:
• Settings -> Network -> HTTP -> Disable 'Default to HTTP/2 if the server supports it'
• Tools -> Proxy -> Proxy listeners -> your listener -> Edit -> HTTP/2 tab -> Disable 'Support HTTP/2'
• Restart Burp and the app";

        throw new ProxyHttp2IncompatibilityException(msg, exception);
    }

    static bool IsProxyEnabled()
        => !string.IsNullOrEmpty(GetProxyEnv());

    static string? GetProxyEnv()
        => Environment.GetEnvironmentVariable("HTTPS_PROXY")
           ?? Environment.GetEnvironmentVariable("HTTP_PROXY")
           ?? Environment.GetEnvironmentVariable("ALL_PROXY");

    static bool LooksLikeHttp2StatusLineIssue(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            var s = e.ToString();
            if (s.Contains("invalid status line", StringComparison.OrdinalIgnoreCase)) return true;
            if (s.Contains("HTTP/2 200 OK", StringComparison.OrdinalIgnoreCase)) return true;
            if (s.Contains("response ended prematurely", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}