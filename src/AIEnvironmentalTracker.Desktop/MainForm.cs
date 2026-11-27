using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace AIEnvironmentalTracker.Desktop;

/// <summary>
/// The application window. It starts the OxyGenAI web server as a hidden process on a free local port,
/// waits until it answers and shows it in a WebView2 control, so the app looks and behaves like a Windows program.
/// Links to other sites open in the default browser; the server stops when the window closes.
/// </summary>
public sealed class MainForm : Form
{
    private const string WebView2DownloadUrl = "https://developer.microsoft.com/microsoft-edge/webview2/";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(90);

    private readonly LaunchOptions options;
    private readonly WebView2 browser = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly Label status = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 12F),
        Text = "Starting OxyGenAI…"
    };

    private ServerProcess? server;
    private bool closing;
    private bool firstPageShown;
    private bool serverReady;   // an exit before this is reported by StartAsync, not by OnServerExited

    public MainForm(LaunchOptions options)
    {
        this.options = options;
        Text = "OxyGenAI";
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(browser);
        Controls.Add(status);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await StartAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        closing = true;
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        server?.Dispose();
        base.OnFormClosed(e);
    }

    private async Task StartAsync()
    {
        try
        {
            var plan = ServerLocator.Find(AppContext.BaseDirectory, Environment.GetEnvironmentVariable(ServerLocator.OverrideVariable));
            if (plan is null)
            {
                Fail("The OxyGenAI server was not found. Expected OxyGenAI.exe next to this program. " +
                     "When running from the source code, build the solution first (dotnet build AIEnvironmentalTracker.slnx).");
                return;
            }

            if (plan.IsPackaged && !await EnsureSettingsAsync(plan)) return;

            var port = ServerProcess.FindFreePort();
            server = new ServerProcess();
            server.Exited += OnServerExited;
            server.Start(plan, port);

            status.Text = "Starting the server…";
            await server.WaitUntilReadyAsync(StartupTimeout);
            serverReady = true;

            status.Text = "Opening the window…";
            await InitializeBrowserAsync(server.Url, port);
        }
        catch (ServerStartException ex)
        {
            Fail($"OxyGenAI could not start.{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                 "Check the internet connection and that Setup OxyGenAI.bat was completed with your Neon connection string (see USER_GUIDE.md, Troubleshooting).");
        }
        catch (WebView2RuntimeNotFoundException)
        {
            Fail($"The Microsoft Edge WebView2 Runtime is not installed. Install it from {WebView2DownloadUrl} and start OxyGenAI again.");
        }
        catch (Exception ex)
        {
            Fail($"OxyGenAI could not start: {ex.Message}");
        }
    }

    // Installed package without a settings file: run the setup script once, as "Start OxyGenAI.bat" does.
    private async Task<bool> EnsureSettingsAsync(ServerLaunchPlan plan)
    {
        var settings = Path.Combine(plan.WorkingDirectory, "oxygenai.settings.json");
        if (File.Exists(settings) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")))
            return true;

        var setup = Path.Combine(plan.WorkingDirectory, "Setup OxyGenAI.bat");
        if (options.IsCheck || !File.Exists(setup))
        {
            Fail("oxygenai.settings.json was not found. Run Setup OxyGenAI.bat first (see USER_GUIDE.md).");
            return false;
        }

        status.Text = "First start: complete the setup in the console window…";
        using var run = Process.Start(new ProcessStartInfo(setup) { UseShellExecute = true, WorkingDirectory = plan.WorkingDirectory });
        if (run is not null) await run.WaitForExitAsync();

        if (File.Exists(settings)) return true;
        Fail("The setup was not completed, so OxyGenAI cannot start. Run Setup OxyGenAI.bat and try again.");
        return false;
    }

    private async Task InitializeBrowserAsync(string url, int port)
    {
        CoreWebView2Environment.GetAvailableBrowserVersionString(); // throws WebView2RuntimeNotFoundException when missing

        var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OxyGenAI", "WebView2");
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: dataFolder);
        await browser.EnsureCoreWebView2Async(environment);

        var web = browser.CoreWebView2;
        web.Settings.AreDevToolsEnabled = options.IsCheck;
        web.Settings.IsStatusBarEnabled = false;

        web.DocumentTitleChanged += (_, _) => Text = string.IsNullOrWhiteSpace(web.DocumentTitle) ? "OxyGenAI" : web.DocumentTitle;

        // Only our own server may load in the window; other addresses open in the default browser.
        web.NavigationStarting += (_, e) =>
        {
            if (UrlPolicy.IsInternal(e.Uri, port)) return;
            e.Cancel = true;
            OpenExternally(e.Uri);
        };
        web.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            if (UrlPolicy.IsInternal(e.Uri, port)) web.Navigate(e.Uri);
            else OpenExternally(e.Uri);
        };

        web.NavigationCompleted += async (_, e) =>
        {
            if (firstPageShown) return;
            firstPageShown = true;

            if (!e.IsSuccess)
            {
                Fail($"The window could not load the page ({e.WebErrorStatus}).");
                return;
            }

            browser.Visible = true;
            status.Visible = false;

            if (options.IsCheck) await WriteCheckResultAsync(web, url);
        };

        web.Navigate(url);
    }

    private async Task WriteCheckResultAsync(CoreWebView2 web, string url)
    {
        var title = (await web.ExecuteScriptAsync("document.title")).Trim('"');
        CheckResult.Write(options.CheckOutputPath, $"OK|{title}|{web.Source}|{url}");
        Environment.ExitCode = 0;
        Close();
    }

    private void OnServerExited(object? sender, EventArgs e)
    {
        if (!serverReady || closing || IsDisposed || !IsHandleCreated) return;
        BeginInvoke(() =>
        {
            if (closing) return;
            var tail = server?.Output.Text;
            Fail("The OxyGenAI server stopped unexpectedly." + (string.IsNullOrEmpty(tail) ? "" : $"{Environment.NewLine}{Environment.NewLine}{tail}"));
        });
    }

    private static void OpenExternally(string uri)
    {
        if (!UrlPolicy.IsSafeExternal(uri)) return;
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { }
    }

    private void Fail(string message)
    {
        closing = true;
        Environment.ExitCode = 1;

        if (options.IsCheck)
            CheckResult.Write(options.CheckOutputPath, "FAIL|" + message.Replace(Environment.NewLine, " "));
        else
            MessageBox.Show(this, message, "OxyGenAI", MessageBoxButtons.OK, MessageBoxIcon.Error);

        Close();
    }
}
