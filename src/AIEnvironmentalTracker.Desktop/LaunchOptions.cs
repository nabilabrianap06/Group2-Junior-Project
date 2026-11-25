namespace AIEnvironmentalTracker.Desktop;

/// <summary>Command-line options of the desktop shell.</summary>
/// <param name="CheckOutputPath">
/// With <c>--check &lt;file&gt;</c> the shell starts the server, loads the first page in WebView2, writes one result line
/// to the file and exits. It is a diagnostic for installers and tests; it needs no user interaction.
/// </param>
public sealed record LaunchOptions(string? CheckOutputPath)
{
    public bool IsCheck => CheckOutputPath is not null;

    public static LaunchOptions Parse(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (!args[i].Equals("--check", StringComparison.OrdinalIgnoreCase)) continue;
            if (i + 1 >= args.Count || string.IsNullOrWhiteSpace(args[i + 1]))
                throw new ArgumentException("--check needs the path of a result file.");
            return new LaunchOptions(args[i + 1]);
        }
        return new LaunchOptions((string?)null);
    }
}
