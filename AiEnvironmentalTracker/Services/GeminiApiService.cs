using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AiEnvironmentalTracker.Interfaces;
using AiEnvironmentalTracker.Models;
using Microsoft.Extensions.Configuration;

namespace AiEnvironmentalTracker.Services
{
    /// <summary>
    /// Talks to the Gemini REST API (gemini-1.5-flash), parses the
    /// response JSON, pulls out the token metadata, and runs the numbers
    /// through <see cref="CalculationEngine"/> before returning.
    /// </summary>
    public class GeminiApiService : IGeminiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelId;
        private readonly CalculationEngine _calculator;

        // reusable serializer settings
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GeminiApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            CalculationEngine calculator)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));

            // read settings from appsettings.json -> "Gemini" section
            _apiKey  = configuration["Gemini:ApiKey"]
                       ?? throw new InvalidOperationException("Gemini:ApiKey is missing from configuration.");
            _modelId = configuration["Gemini:ModelId"] ?? "gemini-1.5-flash";
        }

        public async Task<GeminiChatResult> SendPromptAsync(string userPrompt)
        {
            if (string.IsNullOrWhiteSpace(userPrompt))
                throw new ArgumentException("Prompt cannot be empty.", nameof(userPrompt));

            // ── Build request ───────────────────────────────────────────
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelId}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = userPrompt }
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // ── Call the API ────────────────────────────────────────────
            HttpResponseMessage httpResponse = await _httpClient.PostAsync(url, content);
            httpResponse.EnsureSuccessStatusCode();

            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<GeminiApiResponse>(responseBody, JsonOptions);

            if (parsed == null)
                throw new InvalidOperationException("Failed to deserialize the Gemini API response.");

            // ── Extract text ────────────────────────────────────────────
            string generatedText = string.Empty;
            if (parsed.Candidates is { Length: > 0 })
            {
                var firstCandidate = parsed.Candidates[0];
                if (firstCandidate.Content?.Parts is { Length: > 0 })
                {
                    generatedText = firstCandidate.Content.Parts[0].Text ?? string.Empty;
                }
            }

            // ── Extract token counts ────────────────────────────────────
            int promptTokens     = parsed.UsageMetadata?.PromptTokenCount ?? 0;
            int completionTokens = parsed.UsageMetadata?.CandidatesTokenCount ?? 0;

            var tokenUsage = new UsageToken(promptTokens, completionTokens);

            // ── Compute environmental impact ────────────────────────────
            EnvironmentalImpact impact = _calculator.ComputeImpact(tokenUsage.TotalTokens);

            return new GeminiChatResult
            {
                ResponseText = generatedText,
                TokenUsage   = tokenUsage,
                Impact       = impact
            };
        }

        // ─────────────────────────────────────────────────────────────────
        //  DTOs that mirror the relevant subset of the Gemini JSON payload
        // ─────────────────────────────────────────────────────────────────

        private sealed class GeminiApiResponse
        {
            [JsonPropertyName("candidates")]
            public GeminiCandidate[]? Candidates { get; set; }

            [JsonPropertyName("usageMetadata")]
            public GeminiUsageMetadata? UsageMetadata { get; set; }
        }

        private sealed class GeminiCandidate
        {
            [JsonPropertyName("content")]
            public GeminiContent? Content { get; set; }
        }

        private sealed class GeminiContent
        {
            [JsonPropertyName("parts")]
            public GeminiPart[]? Parts { get; set; }
        }

        private sealed class GeminiPart
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private sealed class GeminiUsageMetadata
        {
            [JsonPropertyName("promptTokenCount")]
            public int PromptTokenCount { get; set; }

            [JsonPropertyName("candidatesTokenCount")]
            public int CandidatesTokenCount { get; set; }

            [JsonPropertyName("totalTokenCount")]
            public int TotalTokenCount { get; set; }
        }
    }
}
