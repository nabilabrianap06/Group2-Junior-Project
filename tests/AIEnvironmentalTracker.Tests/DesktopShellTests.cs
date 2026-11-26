using System.Net;
using System.Net.Sockets;
using System.Text;
using AIEnvironmentalTracker.Desktop;

namespace AIEnvironmentalTracker.Tests;

public class LaunchOptionsTests
{
    [Fact]
    public void NoArguments_MeansNormalStart()
    {
        var options = LaunchOptions.Parse([]);
        Assert.False(options.IsCheck);
        Assert.Null(options.CheckOutputPath);
    }

    [Fact]
    public void Check_TakesTheResultFile()
    {
        var options = LaunchOptions.Parse(["--CHECK", @"C:\temp\result file.txt"]);
        Assert.True(options.IsCheck);
        Assert.Equal(@"C:\temp\result file.txt", options.CheckOutputPath);
    }

    [Theory]
    [InlineData("--check")]
    [InlineData("--check", " ")]
    public void Check_WithoutAFile_IsRejected(params string[] args)
        => Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(args));
}

public class UrlPolicyTests
{
    [Theory]
    [InlineData("http://127.0.0.1:5000/", true)]
    [InlineData("http://127.0.0.1:5000/Account/Login?ReturnUrl=%2F", true)]
    [InlineData("http://localhost:5000/dashboard", true)]
    [InlineData("http://LOCALHOST:5000/", true)]
    [InlineData("http://127.0.0.1:5001/", false)]          // another port
    [InlineData("https://127.0.0.1:5000/", false)]         // other scheme
    [InlineData("http://evil.example:5000/", false)]
    [InlineData("http://127.0.0.1.evil.example:5000/", false)]
    [InlineData("file:///C:/Windows/win.ini", false)]
    [InlineData("about:blank", false)]
    [InlineData("not a url", false)]
    [InlineData(null, false)]
    public void IsInternal_AcceptsOnlyOurOwnServer(string? uri, bool expected)
        => Assert.Equal(expected, UrlPolicy.IsInternal(uri, 5000));

    [Theory]
    [InlineData("https://example.org/page", true)]
    [InlineData("http://example.org", true)]
    [InlineData("file:///C:/Windows/win.ini", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("ms-settings:", false)]
    [InlineData("calc.exe", false)]
    [InlineData(null, false)]
    public void IsSafeExternal_AllowsOnlyWebAddresses(string? uri, bool expected)
        => Assert.Equal(expected, UrlPolicy.IsSafeExternal(uri));
}

public class OutputTailTests
{
    [Fact]
    public void KeepsOnlyTheLastLines_AndSkipsBlankOnes()
    {
        var tail = new OutputTail(maxLines: 3);
        foreach (var line in new[] { "one", "two", "", "  ", null, "three", "four", "five" })
            tail.Add(line);

        Assert.Equal(string.Join(Environment.NewLine, "three", "four", "five"), tail.Text);
    }

    [Fact]
    public void IsEmptyAtFirst() => Assert.Equal("", new OutputTail().Text);

    [Fact]
    public void StackFrames_AreLeftOut_SoTheExplainingMessageStaysVisible()
    {
        var tail = new OutputTail(maxLines: 5);
        tail.Add("crit: Program[0] Could not apply database migrations. Check that PostgreSQL is running.");
        tail.Add("Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:1");
        for (var i = 0; i < 20; i++) tail.Add(new string(' ', 3 + i % 8) + $"at Some.Namespace.Method{i}()");   // any indentation depth
        tail.Add("--- End of stack trace from previous location ---");

        Assert.Contains("Could not apply database migrations", tail.Text);
        Assert.Contains("Failed to connect", tail.Text);
        Assert.DoesNotContain("Some.Namespace.Method", tail.Text);
    }
}

public class ServerLocatorTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "oxy-locator-" + Guid.NewGuid().ToString("N"));

    public ServerLocatorTests() => Directory.CreateDirectory(root);

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Touch(params string[] parts)
    {
        var path = Path.Combine([root, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "");
        return path;
    }

    [Fact]
    public void Packaged_ServerNextToTheShell()
    {
        var exe = Touch("app", "OxyGenAI.exe");
        var plan = ServerLocator.Find(Path.Combine(root, "app"));

        Assert.NotNull(plan);
        Assert.Equal(exe, plan.ExePath);
        Assert.True(plan.IsPackaged);
        Assert.Equal(Path.Combine(root, "app"), plan.WorkingDirectory);
        Assert.Null(plan.EnvironmentName);
    }

    [Fact]
    public void SourceTree_UsesTheNewestBuild_InDevelopment()
    {
        Touch("src", "AIEnvironmentalTracker.Web", "AIEnvironmentalTracker.Web.csproj");
        var older = Touch("src", "AIEnvironmentalTracker.Web", "bin", "Debug", "net10.0", "AIEnvironmentalTracker.Web.exe");
        var newer = Touch("src", "AIEnvironmentalTracker.Web", "bin", "Release", "net10.0", "AIEnvironmentalTracker.Web.exe");
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(newer, DateTime.UtcNow);
        var shellFolder = Path.Combine(root, "src", "AIEnvironmentalTracker.Desktop", "bin", "Debug", "net10.0-windows");
        Directory.CreateDirectory(shellFolder);

        var plan = ServerLocator.Find(shellFolder);

        Assert.NotNull(plan);
        Assert.Equal(newer, plan.ExePath);
        Assert.False(plan.IsPackaged);
        Assert.Equal("Development", plan.EnvironmentName);
        Assert.Equal(Path.Combine(root, "src", "AIEnvironmentalTracker.Web"), plan.WorkingDirectory);
    }

    [Fact]
    public void PackagedServer_WinsOverTheSourceTree()
    {
        Touch("src", "AIEnvironmentalTracker.Web", "AIEnvironmentalTracker.Web.csproj");
        Touch("src", "AIEnvironmentalTracker.Web", "bin", "Debug", "net10.0", "AIEnvironmentalTracker.Web.exe");
        var packaged = Touch("src", "AIEnvironmentalTracker.Desktop", "bin", "OxyGenAI.exe");

        var plan = ServerLocator.Find(Path.GetDirectoryName(packaged)!);

        Assert.True(plan!.IsPackaged);
    }

    [Fact]
    public void OverridePath_IsUsedWhenItExists_AndNullWhenItDoesNot()
    {
        var custom = Touch("custom", "server.exe");
        var plan = ServerLocator.Find(root, custom);
        Assert.Equal(custom, plan!.ExePath);
        Assert.False(plan.IsPackaged);

        Assert.Null(ServerLocator.Find(root, Path.Combine(root, "missing.exe")));
    }

    [Fact]
    public void NothingFound_GivesNull()
    {
        Directory.CreateDirectory(Path.Combine(root, "empty"));
        Assert.Null(ServerLocator.Find(Path.Combine(root, "empty")));
    }

    [Fact]
    public void ProjectWithoutABuild_GivesNull()
    {
        Touch("src", "AIEnvironmentalTracker.Web", "AIEnvironmentalTracker.Web.csproj");
        Assert.Null(ServerLocator.Find(Path.Combine(root, "src")));
    }
}

public class ServerProcessTests
{
    [Fact]
    public void Arguments_FixTheAddress_AndKeepTheSystemBrowserClosed()
    {
        var args = ServerProcess.BuildArguments(5123);
        Assert.Contains("--Kestrel:Endpoints:Http:Url=http://127.0.0.1:5123", args);
        Assert.Contains("--OpenBrowser=false", args);
    }

    [Fact]
    public void FreePorts_AreUsable()
    {
        var port = ServerProcess.FindFreePort();
        Assert.InRange(port, 1024, 65535);

        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();   // would throw if the port were taken
        listener.Stop();
    }

    // A minimal HTTP server: answers every request with a redirect, like the app does for a guest.
    private static (TcpListener Listener, Task Loop) StartFakeServer()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var loop = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    using var client = await listener.AcceptTcpClientAsync();
                    var stream = client.GetStream();
                    var buffer = new byte[2048];
                    _ = await stream.ReadAsync(buffer);
                    var reply = Encoding.ASCII.GetBytes("HTTP/1.1 302 Found\r\nLocation: /login\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(reply);
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException or SocketException or InvalidOperationException) { }
        });
        return (listener, loop);
    }

    [Fact]
    public async Task WaitUntilReady_ReturnsOnceTheServerAnswers_AnyStatusCounts()
    {
        var (listener, _) = StartFakeServer();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            await ServerProcess.WaitUntilReadyAsync($"http://127.0.0.1:{port}/", TimeSpan.FromSeconds(10), () => false);
        }
        finally { listener.Stop(); }
    }

    [Fact]
    public async Task WaitUntilReady_KeepsTryingUntilTheServerComesUp()
    {
        var port = ServerProcess.FindFreePort();
        var wait = ServerProcess.WaitUntilReadyAsync($"http://127.0.0.1:{port}/", TimeSpan.FromSeconds(15), () => false);

        await Task.Delay(800);                       // the server is not up yet
        Assert.False(wait.IsCompleted);

        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        var accept = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            var stream = client.GetStream();
            _ = await stream.ReadAsync(new byte[2048]);
            await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"));
        });
        try { await wait; }
        finally { listener.Stop(); }
        await accept;
    }

    [Fact]
    public async Task WaitUntilReady_FailsAtOnce_WhenTheServerHasExited()
    {
        var port = ServerProcess.FindFreePort();
        var ex = await Assert.ThrowsAsync<ServerStartException>(
            () => ServerProcess.WaitUntilReadyAsync($"http://127.0.0.1:{port}/", TimeSpan.FromSeconds(30), () => true));
        Assert.Contains("stopped", ex.Message);
    }

    [Fact]
    public async Task WaitUntilReady_GivesUpAfterTheTimeout()
    {
        var port = ServerProcess.FindFreePort();
        var ex = await Assert.ThrowsAsync<ServerStartException>(
            () => ServerProcess.WaitUntilReadyAsync($"http://127.0.0.1:{port}/", TimeSpan.FromSeconds(1), () => false));
        Assert.Contains("did not answer", ex.Message);
    }

    [Fact]
    public async Task WaitUntilReady_CanBeCancelled()
    {
        var port = ServerProcess.FindFreePort();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ServerProcess.WaitUntilReadyAsync($"http://127.0.0.1:{port}/", TimeSpan.FromSeconds(30), () => false, cts.Token));
    }
}
