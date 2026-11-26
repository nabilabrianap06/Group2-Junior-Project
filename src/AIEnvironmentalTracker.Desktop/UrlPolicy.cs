namespace AIEnvironmentalTracker.Desktop;

/// <summary>Decides which addresses may open inside the window; everything else goes to the default browser.</summary>
public static class UrlPolicy
{
    public static string BaseUrl(int port) => $"http://127.0.0.1:{port}/";

    /// <summary>True for pages of our own server (any path on 127.0.0.1 or localhost with our port).</summary>
    public static bool IsInternal(string? uri, int port)
        => Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
           && parsed.Scheme == Uri.UriSchemeHttp
           && parsed.Port == port
           && (parsed.Host == "127.0.0.1" || parsed.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));

    /// <summary>True for web addresses that are safe to hand to the default browser (http and https only).</summary>
    public static bool IsSafeExternal(string? uri)
        => Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
           && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
}
