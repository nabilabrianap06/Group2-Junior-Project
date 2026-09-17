using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AIEnvironmentalTracker.Domain.Models;

public class AIUsageLog
{
    // Attributes matching UML
    public int LogId { get; set; }
    public string DetectedApp { get; set; } = string.Empty;
    public double SessionDuration { get; set; }
    public int QueryCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Foreign Key / Navigation back to RegisteredUser
    public int RegisteredUserId { get; set; }
    public RegisteredUser? RegisteredUser { get; set; }

    // Desktop/CLI Applications mapped to normalized names
    private static readonly Dictionary<string, string> NativeAiApps = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ChatGPT", "ChatGPT Desktop" },
        { "Claude", "Claude Desktop" },
        { "lm-studio", "LM Studio" },
        { "ollama", "Ollama Local Engine" },
        { "jan", "Jan AI" },
        { "copilot", "Microsoft Copilot" }
    };

    // Web-based AI Models detectable via browser main window titles
    private static readonly Dictionary<string, string> WebAiKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Gemini", "Google Gemini Web" },
        { "ChatGPT", "ChatGPT Web" },
        { "Claude", "Claude Web" },
        { "Perplexity", "Perplexity AI Web" },
        { "DeepSeek", "DeepSeek Web" },
        { "HuggingChat", "Hugging Chat Web" },
        { "Poe", "Poe AI Web" }
    };

    // Known browser executables
    private static readonly HashSet<string> Browsers = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome", "msedge", "firefox", "brave", "opera", "vivaldi"
    };

    /// Scans current host system processes to identify active AI applications or browser tabs.
    public void ScanDeviceActivity()
    {
        Process[] runningProcesses = Process.GetProcesses();

        foreach (var process in runningProcesses)
        {
            try
            {
                string processName = process.ProcessName;

                if (NativeAiApps.TryGetValue(processName, out string? nativeAppName))
                {
                    DetectedApp = nativeAppName;
                    CalculateSessionDuration(process);
                    return;
                }

                if (Browsers.Contains(processName) && !string.IsNullOrWhiteSpace(process.MainWindowTitle))
                {
                    foreach (var (keyword, modelName) in WebAiKeywords)
                    {
                        if (process.MainWindowTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            DetectedApp = modelName;
                            CalculateSessionDuration(process);
                            return;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Access restricted system processes are ignored
            }
        }

        if (string.IsNullOrEmpty(DetectedApp))
        {
            DetectedApp = "No Active AI App Detected";
        }
    }

    /// Encapsulates log creation/persistence setup logic.
    public void CreateUsageLog()
    {
        Timestamp = DateTime.UtcNow;
        if (QueryCount <= 0)
        {
            QueryCount = 1; // Default initial query baseline if unassigned
        }
    }

    /// Computes environmental impact by linking this log instance to the ImpactCalculator.
    public ImpactCalculator CalculateImpact(EnvironmentalFactor factors)
    {
        var calculator = new ImpactCalculator();
        calculator.ComputeImpact(this, factors);
        return calculator;
    }

    private void CalculateSessionDuration(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                SessionDuration = Math.Max(0, (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalMinutes);
            }
        }
        catch (Exception)
        {
            // Fallback duration when process start time is inaccessible due to permissions
            SessionDuration = 1.0; 
        }
    }
}
