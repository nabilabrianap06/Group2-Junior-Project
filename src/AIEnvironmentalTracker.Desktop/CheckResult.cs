namespace AIEnvironmentalTracker.Desktop;

/// <summary>Writes the one-line result of <c>--check</c>. A write error must never turn into another crash.</summary>
internal static class CheckResult
{
    public static void Write(string? path, string line)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            File.WriteAllText(path, line);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"Could not write the result file '{path}': {ex.Message}");
        }
    }
}
