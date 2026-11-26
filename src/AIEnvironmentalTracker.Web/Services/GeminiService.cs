using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AIEnvironmentalTracker.Web.Services;

/// <summary>Settings bound from the "Gemini" configuration section.</summary>
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Set with user-secrets ("Gemini:ApiKey") or the environment variable Gemini__ApiKey. Never in appsettings.json.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The default model, used when the caller does not pick one.</summary>
    public string Model { get; set; } = "gemini-3.5-flash-lite";

    /// <summary>
    /// Models a user may choose. Only these ids are ever sent to Google, so a user cannot make the app call
    /// an arbitrary (possibly paid) model. The default <see cref="Model"/> is always allowed.
    /// </summary>
    public List<string> AllowedModels { get; set; } = [];

    /// <summary>Longest prompt accepted, in characters. Enforced here, not only in the browser.</summary>
    public int MaxPromptCharacters { get; set; } = 4000;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
}

/// <summary>Text reply and token usage returned by one generateContent call.</summary>
public sealed record GeminiReply(string Text, string Model, int PromptTokens, int CandidateTokens, int TotalTokens);

/// <summary>A failure with a message that is safe to show to the user.</summary>
public class GeminiException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Calls the Google Gemini REST API (models/{model}:generateContent).</summary>
public class GeminiService(HttpClient http, IOptions<GeminiOptions> options, ILogger<GeminiService> logger)
{
    public const string MissingKeyMessage =
        "The Gemini API key is not set, so Ask Gemini is turned off. Installed app: run \"Setup OxyGenAI.bat\", " +
        "enter your key, then restart OxyGenAI. From source: dotnet user-secrets set \"Gemini:ApiKey\" \"YOUR_KEY\" " +
        "--project src/AIEnvironmentalTracker.Web, or set the environment variable Gemini__ApiKey.";

    private readonly GeminiOptions settings = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.ApiKey);

    /// <summary>The default model.</summary>
    public string Model => settings.Model;

    public int MaxPromptCharacters => settings.MaxPromptCharacters;

    /// <summary>The models a user may choose, default first, without duplicates.</summary>
    public IReadOnlyList<string> AvailableModels
        => new[] { settings.Model }
            .Concat(settings.AllowedModels)
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Sends <paramref name="prompt"/> to <paramref name="model"/> (the default model when null).</summary>
    public async Task<GeminiReply> GenerateAsync(string prompt, string? model = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new GeminiException("Please enter a prompt.");
        if (prompt.Length > settings.MaxPromptCharacters)
            throw new GeminiException($"The prompt is too long. The limit is {settings.MaxPromptCharacters:N0} characters.");
        if (!IsConfigured)
            throw new GeminiException(MissingKeyMessage);

        model = string.IsNullOrWhiteSpace(model) ? settings.Model : model.Trim();
        var allowed = AvailableModels.FirstOrDefault(candidate => candidate.Equals(model, StringComparison.OrdinalIgnoreCase));
        if (allowed is null)
            throw new GeminiException("This model is not available. Choose one from the list.");
        model = allowed;

        var url = new Uri(new Uri(settings.BaseUrl), $"models/{Uri.EscapeDataString(model)}:generateContent");
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new GenerateRequest([new Content("user", [new Part(prompt)])]))
        };
        // Sent as a header, not in the URL, so the key does not end up in logs.
        request.Headers.Add("x-goog-api-key", settings.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeminiException("Gemini did not answer in time. Please try again.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Network error calling Gemini");
            throw new GeminiException("Could not reach the Gemini API. Check your internet connection and try again.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Gemini returned {StatusCode}: {Body}", (int)response.StatusCode, Truncate(body, 500));
                throw new GeminiException(DescribeError(response.StatusCode, body));
            }

            GenerateResponse? result;
            try
            {
                result = await response.Content.ReadFromJsonAsync<GenerateResponse>(cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new GeminiException("Gemini sent a response the app could not read.", ex);
            }

            var candidate = result?.Candidates?.FirstOrDefault();
            var text = string.Concat(candidate?.Content?.Parts?.Select(part => part.Text) ?? []);
            if (string.IsNullOrWhiteSpace(text))
            {
                var reason = result?.PromptFeedback?.BlockReason ?? candidate?.FinishReason ?? "no text returned";
                text = $"(Gemini returned no text: {reason})";
            }

            var usage = result?.UsageMetadata;
            var promptTokens = usage?.PromptTokenCount ?? 0;
            var candidateTokens = usage?.CandidatesTokenCount ?? 0;
            var totalTokens = usage?.TotalTokenCount ?? promptTokens + candidateTokens;

            return new GeminiReply(text, model, promptTokens, candidateTokens, totalTokens);
        }
    }

    private static string DescribeError(HttpStatusCode status, string body) => status switch
    {
        HttpStatusCode.TooManyRequests =>
            "Gemini rate limit or quota reached. Wait a minute and try again.",
        HttpStatusCode.BadRequest when body.Contains("API_KEY_INVALID", StringComparison.Ordinal) =>
            "The Gemini API key was rejected. Check the value of Gemini:ApiKey.",
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
            "The Gemini API key is not allowed to use this model. Check the key and its project settings.",
        HttpStatusCode.NotFound =>
            "The configured Gemini model was not found. Check the Gemini:Model setting.",
        HttpStatusCode.BadRequest =>
            "Gemini could not process this request. Try a shorter or different prompt.",
        >= HttpStatusCode.InternalServerError =>
            "Gemini is temporarily unavailable. Please try again later.",
        _ => $"Gemini returned an error ({(int)status}). Please try again."
    };

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    // Request and response shapes for generateContent (only the fields the app uses).
    private sealed record GenerateRequest([property: JsonPropertyName("contents")] IReadOnlyList<Content> Contents);

    private sealed record Content(
        [property: JsonPropertyName("role")] string? Role,
        [property: JsonPropertyName("parts")] IReadOnlyList<Part>? Parts);

    private sealed record Part([property: JsonPropertyName("text")] string? Text);

    private sealed record GenerateResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<Candidate>? Candidates,
        [property: JsonPropertyName("promptFeedback")] PromptFeedback? PromptFeedback,
        [property: JsonPropertyName("usageMetadata")] UsageMetadata? UsageMetadata,
        [property: JsonPropertyName("modelVersion")] string? ModelVersion);

    private sealed record Candidate(
        [property: JsonPropertyName("content")] Content? Content,
        [property: JsonPropertyName("finishReason")] string? FinishReason);

    private sealed record PromptFeedback([property: JsonPropertyName("blockReason")] string? BlockReason);

    private sealed record UsageMetadata(
        [property: JsonPropertyName("promptTokenCount")] int? PromptTokenCount,
        [property: JsonPropertyName("candidatesTokenCount")] int? CandidatesTokenCount,
        [property: JsonPropertyName("totalTokenCount")] int? TotalTokenCount);
}
