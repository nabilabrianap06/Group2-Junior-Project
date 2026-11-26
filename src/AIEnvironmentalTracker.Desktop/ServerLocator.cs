namespace AIEnvironmentalTracker.Desktop;

/// <summary>How to start the web server: which program, in which folder and (for development) in which environment.</summary>
/// <param name="ExePath">The server executable.</param>
/// <param name="WorkingDirectory">Folder the server starts in. It also decides where the server looks for its settings and web assets.</param>
/// <param name="IsPackaged">True for the installed package (server next to the shell); false when running from the source tree.</param>
/// <param name="EnvironmentName">Value for ASPNETCORE_ENVIRONMENT, or null to keep the default (Production).</param>
public sealed record ServerLaunchPlan(string ExePath, string WorkingDirectory, bool IsPackaged, string? EnvironmentName);

/// <summary>Finds the web server the shell should run.</summary>
public static class ServerLocator
{
    public const string PackagedServerFileName = "OxyGenAI.exe";
    public const string SourceServerFileName = "AIEnvironmentalTracker.Web.exe";
    public const string OverrideVariable = "OXYGENAI_SERVER";

    /// <summary>
    /// 1) the path in OXYGENAI_SERVER, 2) <c>OxyGenAI.exe</c> next to the shell (installed package),
    /// 3) the newest build of the web project found by walking up from the shell (running from the source tree,
    /// in the Development environment so that user-secrets and the build's web assets are used).
    /// Returns null when nothing is found.
    /// </summary>
    public static ServerLaunchPlan? Find(string baseDirectory, string? overridePath = null)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (!File.Exists(overridePath)) return null;
            var dir = Path.GetDirectoryName(Path.GetFullPath(overridePath))!;
            return new ServerLaunchPlan(Path.GetFullPath(overridePath), dir, IsPackaged: false, EnvironmentName: null);
        }

        var packaged = Path.Combine(baseDirectory, PackagedServerFileName);
        if (File.Exists(packaged))
            return new ServerLaunchPlan(packaged, baseDirectory, IsPackaged: true, EnvironmentName: null);

        var project = FindWebProjectDirectory(baseDirectory);
        if (project is null) return null;

        var exe = Directory.Exists(Path.Combine(project, "bin"))
            ? Directory.EnumerateFiles(Path.Combine(project, "bin"), SourceServerFileName, SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        return exe is null
            ? null
            : new ServerLaunchPlan(exe, project, IsPackaged: false, EnvironmentName: "Development");
    }

    private static string? FindWebProjectDirectory(string start)
    {
        var current = new DirectoryInfo(start);
        for (var depth = 0; current is not null && depth < 10; depth++, current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, "src", "AIEnvironmentalTracker.Web");
            if (File.Exists(Path.Combine(candidate, "AIEnvironmentalTracker.Web.csproj")))
                return candidate;
        }
        return null;
    }
}
