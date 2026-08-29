using System.Threading.Tasks;
using AiEnvironmentalTracker.Models;

namespace AiEnvironmentalTracker.Interfaces
{
    /// <summary>
    /// Abstraction over the Gemini REST API so the rest of the app
    /// doesn't depend on HTTP details or JSON parsing.
    /// </summary>
    public interface IGeminiApiService
    {
        /// <summary>
        /// Sends a user prompt to Gemini and returns the response text
        /// together with token usage and computed environmental metrics.
        /// </summary>
        Task<GeminiChatResult> SendPromptAsync(string userPrompt);
    }
}
