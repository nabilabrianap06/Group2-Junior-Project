namespace AIEnvironmentalTracker.Desktop;

/// <summary>
/// Keeps the last lines of a process's output, so a start-up failure can be shown to the user.
/// Stack-trace frames ("   at Namespace.Method") are left out: they would push the explaining message out of view.
/// </summary>
public sealed partial class OutputTail(int maxLines = 25)
{
    private readonly Queue<string> lines = new();
    private readonly object gate = new();

    public void Add(string? line)
    {
        if (string.IsNullOrWhiteSpace(line) || IsStackFrame(line)) return;
        lock (gate)
        {
            lines.Enqueue(line.TrimEnd());
            while (lines.Count > maxLines) lines.Dequeue();
        }
    }

    private static bool IsStackFrame(string line)
        => StackFrame().IsMatch(line) || line.TrimStart().StartsWith("--- End of", StringComparison.Ordinal);

    // An indented line that starts with "at ", as the runtime prints stack frames (any depth of indentation).
    [System.Text.RegularExpressions.GeneratedRegex(@"^\s+at\s\S", System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
    private static partial System.Text.RegularExpressions.Regex StackFrame();

    public string Text
    {
        get { lock (gate) return string.Join(Environment.NewLine, lines); }
    }
}
