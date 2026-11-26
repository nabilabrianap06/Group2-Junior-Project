using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace AIEnvironmentalTracker.Desktop;

/// <summary>The web server could not be started, or stopped before it was ready. The message is safe to show.</summary>
public sealed class ServerStartException(string message) : Exception(message);

/// <summary>Runs the web server as a hidden child process on a free local port and waits until it answers.</summary>
public sealed class ServerProcess : IDisposable
{
    private readonly KillOnCloseJob job = new();
    private Process? process;

    public OutputTail Output { get; } = new();

    public int Port { get; private set; }

    public string Url => UrlPolicy.BaseUrl(Port);

    /// <summary>Raised on a background thread when the server process ends.</summary>
    public event EventHandler? Exited;

    public bool HasExited => process is null || process.HasExited;

    /// <summary>A free TCP port on the loopback address.</summary>
    public static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try { return ((IPEndPoint)listener.LocalEndpoint).Port; }
        finally { listener.Stop(); }
    }

    /// <summary>
    /// Command-line configuration for the server. Command-line values win over every settings file, so the server
    /// listens only on our port and does not open the system browser (the window already shows the page).
    /// </summary>
    public static IReadOnlyList<string> BuildArguments(int port)
        => [$"--Kestrel:Endpoints:Http:Url=http://127.0.0.1:{port}", "--OpenBrowser=false"];

    public void Start(ServerLaunchPlan plan, int port)
    {
        if (process is not null) throw new InvalidOperationException("The server was already started.");
        Port = port;

        var info = new ProcessStartInfo(plan.ExePath)
        {
            WorkingDirectory = plan.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in BuildArguments(port)) info.ArgumentList.Add(argument);
        if (plan.EnvironmentName is not null) info.Environment["ASPNETCORE_ENVIRONMENT"] = plan.EnvironmentName;

        process = new Process { StartInfo = info, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => Output.Add(e.Data);
        process.ErrorDataReceived += (_, e) => Output.Add(e.Data);
        process.Exited += (_, _) => Exited?.Invoke(this, EventArgs.Empty);

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw new ServerStartException($"The server program could not be started: {ex.Message}");
        }

        job.Add(process);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    /// <summary>Waits until the server answers HTTP, or throws with the server's last output if it stops or takes too long.</summary>
    public async Task WaitUntilReadyAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (process is null) throw new InvalidOperationException("The server was not started.");

        try
        {
            await WaitUntilReadyAsync(Url, timeout, () => process.HasExited, cancellationToken);
        }
        catch (ServerStartException ex)
        {
            var tail = Output.Text;
            throw new ServerStartException(string.IsNullOrEmpty(tail) ? ex.Message : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}{tail}");
        }
    }

    /// <summary>Polls <paramref name="url"/> until any HTTP response arrives (a redirect to the login page counts).</summary>
    public static async Task WaitUntilReadyAsync(string url, TimeSpan timeout, Func<bool> hasExited, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (hasExited())
                throw new ServerStartException("The server stopped while starting.");

            try
            {
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                return;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { }

            if (DateTime.UtcNow >= deadline)
                throw new ServerStartException($"The server did not answer within {timeout.TotalSeconds:0} seconds.");

            await Task.Delay(250, cancellationToken);
        }
    }

    public void Dispose()
    {
        try
        {
            if (process is { HasExited: false })
                process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }

        process?.Dispose();
        job.Dispose();
    }
}
