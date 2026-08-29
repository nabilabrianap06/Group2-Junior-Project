using System;

namespace AiEnvironmentalTracker.Models
{
    /// <summary>
    /// Aggregates everything we get back from a single Gemini API call:
    /// the generated text, the raw token counts, and the derived eco-metrics.
    /// Passed up from the service layer to whoever triggered the chat.
    /// </summary>
    public class GeminiChatResult
    {
        public string ResponseText { get; set; } = string.Empty;
        public UsageToken TokenUsage { get; set; } = new();
        public EnvironmentalImpact Impact { get; set; } = new();
    }
}
