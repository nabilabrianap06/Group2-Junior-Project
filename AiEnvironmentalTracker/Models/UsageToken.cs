using System;

namespace AiEnvironmentalTracker.Models
{
    /// <summary>
    /// Represents the token usage breakdown returned by the Gemini API
    /// after processing a prompt and generating a response.
    /// </summary>
    public class UsageToken
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }

        public UsageToken() { }

        public UsageToken(int promptTokens, int completionTokens)
        {
            PromptTokens = promptTokens;
            CompletionTokens = completionTokens;
            TotalTokens = promptTokens + completionTokens;
        }
    }
}
