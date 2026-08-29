using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AiEnvironmentalTracker.Interfaces;
using AiEnvironmentalTracker.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiEnvironmentalTracker.Services
{
    public class AiProxyService : IAiProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly IProxyRouter _router;
        private readonly CalculationEngine _calculator;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiProxyService> _logger;

        public AiProxyService(
            HttpClient httpClient,
            IProxyRouter router,
            CalculationEngine calculator,
            IServiceScopeFactory scopeFactory,
            ILogger<AiProxyService> logger)
        {
            _httpClient = httpClient;
            _router = router;
            _calculator = calculator;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task HandleChatCompletionsAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // 1. Read incoming request body
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            string requestBodyText = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBodyText))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = new { message = "Request body is empty." } });
                return;
            }

            // 2. Parse request JSON to inspect model, stream flag, and prompt content
            string modelName = "default";
            bool isStreaming = false;
            int estimatedPromptTokens = 0;

            try
            {
                using var jsonDoc = JsonDocument.Parse(requestBodyText);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("model", out var modelProp))
                {
                    modelName = modelProp.GetString() ?? "default";
                }

                if (root.TryGetProperty("stream", out var streamProp))
                {
                    isStreaming = streamProp.ValueKind == JsonValueKind.True || (streamProp.ValueKind == JsonValueKind.String && streamProp.GetString() == "true");
                }

                // Rough estimation of prompt tokens based on character count if upstream doesn't return usage
                if (root.TryGetProperty("messages", out var messagesProp) && messagesProp.ValueKind == JsonValueKind.Array)
                {
                    int totalChars = 0;
                    foreach (var msg in messagesProp.EnumerateArray())
                    {
                        if (msg.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
                        {
                            totalChars += contentProp.GetString()?.Length ?? 0;
                        }
                    }
                    estimatedPromptTokens = Math.Max(1, totalChars / 4);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to inspect request body JSON.");
            }

            // 3. Resolve upstream route (provider, URL, API key)
            UpstreamRoute route = _router.ResolveRoute(modelName, context.Request);

            _logger.LogInformation("Proxying request for model '{Model}' to provider '{Provider}' ({Url}) [Streaming: {IsStreaming}]",
                modelName, route.Provider, route.UpstreamUrl, isStreaming);

            // 4. Branch based on streaming mode
            if (isStreaming)
            {
                await HandleStreamingProxyAsync(context, requestBodyText, route, modelName, estimatedPromptTokens, stopwatch);
            }
            else
            {
                await HandleNonStreamingProxyAsync(context, requestBodyText, route, modelName, estimatedPromptTokens, stopwatch);
            }
        }

        // ── Non-Streaming Proxy ──────────────────────────────────────────

        private async Task HandleNonStreamingProxyAsync(
            HttpContext context,
            string requestBodyText,
            UpstreamRoute route,
            string modelName,
            int estimatedPromptTokens,
            Stopwatch stopwatch)
        {
            using var upstreamReq = new HttpRequestMessage(HttpMethod.Post, route.UpstreamUrl);
            upstreamReq.Content = new StringContent(requestBodyText, Encoding.UTF8, "application/json");

            ApplyHeaders(upstreamReq, route);

            HttpResponseMessage upstreamResp;
            try
            {
                upstreamResp = await _httpClient.SendAsync(upstreamReq, HttpCompletionOption.ResponseContentRead, context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed forwarding request to upstream provider '{Provider}'.", route.Provider);
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                await context.Response.WriteAsJsonAsync(new { error = new { message = $"Gateway error forwarding to {route.Provider}: {ex.Message}" } });
                return;
            }

            stopwatch.Stop();
            long latencyMs = stopwatch.ElapsedMilliseconds;

            string responseBodyText = await upstreamResp.Content.ReadAsStringAsync();

            // Extract usage tokens from response JSON
            int promptTokens = estimatedPromptTokens;
            int completionTokens = 0;
            int totalTokens = 0;

            if (upstreamResp.IsSuccessStatusCode)
            {
                try
                {
                    using var respJson = JsonDocument.Parse(responseBodyText);
                    var root = respJson.RootElement;

                    if (root.TryGetProperty("usage", out var usageProp))
                    {
                        if (usageProp.TryGetProperty("prompt_tokens", out var pTok)) promptTokens = pTok.GetInt32();
                        if (usageProp.TryGetProperty("completion_tokens", out var cTok)) completionTokens = cTok.GetInt32();
                        if (usageProp.TryGetProperty("total_tokens", out var tTok)) totalTokens = tTok.GetInt32();
                    }
                    else if (root.TryGetProperty("usageMetadata", out var geminiUsage))
                    {
                        if (geminiUsage.TryGetProperty("promptTokenCount", out var pTok)) promptTokens = pTok.GetInt32();
                        if (geminiUsage.TryGetProperty("candidatesTokenCount", out var cTok)) completionTokens = cTok.GetInt32();
                        if (geminiUsage.TryGetProperty("totalTokenCount", out var tTok)) totalTokens = tTok.GetInt32();
                    }

                    if (totalTokens == 0)
                    {
                        totalTokens = promptTokens + completionTokens;
                        if (totalTokens == 0)
                        {
                            totalTokens = Math.Max(1, responseBodyText.Length / 4 + estimatedPromptTokens);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse usage from upstream response.");
                    totalTokens = Math.Max(1, responseBodyText.Length / 4 + estimatedPromptTokens);
                }

                // Asynchronously record environmental telemetry without blocking response
                RecordTelemetryInBackground(route.Provider, modelName, promptTokens, completionTokens, totalTokens, latencyMs, isStreaming: false, (int)upstreamResp.StatusCode);
            }

            // Return upstream response to client
            context.Response.StatusCode = (int)upstreamResp.StatusCode;
            context.Response.ContentType = upstreamResp.Content.Headers.ContentType?.ToString() ?? "application/json";
            await context.Response.WriteAsync(responseBodyText);
        }

        // ── Streaming Proxy (Server-Sent Events) ─────────────────────────

        private async Task HandleStreamingProxyAsync(
            HttpContext context,
            string requestBodyText,
            UpstreamRoute route,
            string modelName,
            int estimatedPromptTokens,
            Stopwatch stopwatch)
        {
            using var upstreamReq = new HttpRequestMessage(HttpMethod.Post, route.UpstreamUrl);
            upstreamReq.Content = new StringContent(requestBodyText, Encoding.UTF8, "application/json");

            ApplyHeaders(upstreamReq, route);

            HttpResponseMessage upstreamResp;
            try
            {
                upstreamResp = await _httpClient.SendAsync(upstreamReq, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate stream with upstream provider '{Provider}'.", route.Provider);
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                await context.Response.WriteAsJsonAsync(new { error = new { message = $"Gateway streaming error connecting to {route.Provider}: {ex.Message}" } });
                return;
            }

            if (!upstreamResp.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                string errorBody = await upstreamResp.Content.ReadAsStringAsync();
                context.Response.StatusCode = (int)upstreamResp.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(errorBody);
                return;
            }

            // Set SSE response headers for real-time unbuffered client delivery
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/event-stream; charset=utf-8";
            context.Response.Headers["Cache-Control"] = "no-cache";
            context.Response.Headers["Connection"] = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";

            int promptTokens = estimatedPromptTokens;
            int completionTokens = 0;
            int totalTokens = 0;
            int generatedCharCount = 0;

            using var upstreamStream = await upstreamResp.Content.ReadAsStreamAsync(context.RequestAborted);
            using var streamReader = new StreamReader(upstreamStream, Encoding.UTF8);

            string? line;
            while ((line = await streamReader.ReadLineAsync(context.RequestAborted)) != null)
            {
                // Relay line immediately to client
                byte[] lineBytes = Encoding.UTF8.GetBytes(line + "\n");
                await context.Response.Body.WriteAsync(lineBytes, 0, lineBytes.Length, context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);

                // Inspect SSE data payload for usage telemetry
                if (line.StartsWith("data: ") && line.Length > 6)
                {
                    string dataPayload = line.Substring(6).Trim();
                    if (dataPayload != "[DONE]")
                    {
                        try
                        {
                            using var chunkJson = JsonDocument.Parse(dataPayload);
                            var root = chunkJson.RootElement;

                            // Check if stream returned usage chunk (OpenAI stream_options: { include_usage: true })
                            if (root.TryGetProperty("usage", out var usageProp) && usageProp.ValueKind == JsonValueKind.Object)
                            {
                                if (usageProp.TryGetProperty("prompt_tokens", out var pTok)) promptTokens = pTok.GetInt32();
                                if (usageProp.TryGetProperty("completion_tokens", out var cTok)) completionTokens = cTok.GetInt32();
                                if (usageProp.TryGetProperty("total_tokens", out var tTok)) totalTokens = tTok.GetInt32();
                            }

                            // Extract text delta to track length
                            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var choice in choices.EnumerateArray())
                                {
                                    if (choice.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var contentStr))
                                    {
                                        generatedCharCount += contentStr.GetString()?.Length ?? 0;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Skip JSON parse errors for non-standard chunks
                        }
                    }
                }
            }

            stopwatch.Stop();
            long latencyMs = stopwatch.ElapsedMilliseconds;

            if (totalTokens == 0)
            {
                completionTokens = Math.Max(1, generatedCharCount / 4);
                totalTokens = promptTokens + completionTokens;
            }

            // Asynchronously record environmental telemetry in background
            RecordTelemetryInBackground(route.Provider, modelName, promptTokens, completionTokens, totalTokens, latencyMs, isStreaming: true, StatusCodes.Status200OK);
        }

        // ── Helper: Background Telemetry Logging ────────────────────────

        private void RecordTelemetryInBackground(
            string provider,
            string modelName,
            int promptTokens,
            int completionTokens,
            int totalTokens,
            long latencyMs,
            bool isStreaming,
            int statusCode)
        {
            Task.Run(async () =>
            {
                try
                {
                    EnvironmentalImpact impact = _calculator.ComputeImpact(totalTokens);

                    var logEntry = new AiUsageLog
                    {
                        Id = Guid.NewGuid(),
                        Provider = provider,
                        ModelName = modelName,
                        PromptTokens = promptTokens,
                        CompletionTokens = completionTokens,
                        TotalTokens = totalTokens,
                        EnergyKWh = impact.EnergyKWh,
                        CarbonGrams = impact.CarbonGrams,
                        WaterML = impact.WaterML,
                        AnalogyString = impact.AnalogyString,
                        LatencyMs = latencyMs,
                        IsStreaming = isStreaming,
                        StatusCode = statusCode,
                        CreatedAt = DateTime.UtcNow
                    };

                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IImpactRepository>();
                    await repo.SaveUsageLogAsync(logEntry);

                    _logger.LogInformation("[Telemetry] Logged {Tokens} tokens ({KWh} kWh, {CO2}g CO2) for {Provider}/{Model} in {Latency}ms",
                        totalTokens, impact.EnergyKWh, impact.CarbonGrams, provider, modelName, latencyMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed saving telemetry log to PostgreSQL database.");
                }
            });
        }

        private static void ApplyHeaders(HttpRequestMessage req, UpstreamRoute route)
        {
            if (!string.IsNullOrWhiteSpace(route.ResolvedApiKey))
            {
                req.Headers.TryAddWithoutValidation("Authorization", route.ResolvedApiKey);
            }

            foreach (var kv in route.Headers)
            {
                req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            }
        }
    }
}
