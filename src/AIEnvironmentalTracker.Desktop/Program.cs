using AIEnvironmentalTracker.Desktop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        LaunchOptions options;
        try
        {
            options = LaunchOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "OxyGenAI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 2;
        }

        ApplicationConfiguration.Initialize();

        // An unexpected error in an event handler must not leave a hidden dialog or a half-working window:
        // report it (to the check file in --check mode) and end the program. The server stops with the shell.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        // Details go to the standard error stream when one is attached (for example by an installer or a test).
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Console.Error.WriteLine(e.ExceptionObject);
        Application.ThreadException += (_, e) =>
        {
            Console.Error.WriteLine(e.Exception);
            if (options.IsCheck)
                CheckResult.Write(options.CheckOutputPath, "FAIL|unexpected error: " + e.Exception.Message.Replace(Environment.NewLine, " "));
            else
                MessageBox.Show($"OxyGenAI hit an unexpected error and will close.{Environment.NewLine}{Environment.NewLine}{e.Exception.Message}",
                    "OxyGenAI", MessageBoxButtons.OK, MessageBoxIcon.Error);

            Environment.Exit(1);
        };

        Application.Run(new MainForm(options));
        return Environment.ExitCode;
    }
}
