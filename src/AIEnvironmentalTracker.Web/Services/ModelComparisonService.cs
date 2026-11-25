using AIEnvironmentalTracker.Domain.Models;
using AIEnvironmentalTracker.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace AIEnvironmentalTracker.Web.Services;

/// <summary>
/// The outcome for one model in a comparison. <see cref="Reply"/> and <see cref="Impact"/> are set when the model
/// answered; <see cref="Error"/> explains a failure. <see cref="Rank"/> is 1 for the lowest estimated impact and 0 for a failed model.
/// </summary>
public sealed record ModelComparisonRow(
    string Model,
    int Rank,
    GeminiReply? Reply,
    ImpactEstimate? Impact,
    decimal? EfficiencyScore,
    string? Error,
    string? SaveError)
{
    /// <summary>Tokens the model used beyond the prompt and the visible answer (internal "thinking").</summary>
    public int ThinkingTokens => Reply is null ? 0 : Math.Max(0, Reply.TotalTokens - Reply.PromptTokens - Reply.CandidateTokens);
}

/// <summary>
/// Sends the same prompt to several Gemini models and ranks them by estimated environmental impact.
/// All models get the same energy-per-token assumption (Google publishes no per-model figure), so the ranking
/// reflects how many tokens each model needed for this prompt, not a difference in energy per token.
/// Each successful answer is a real request and is saved to the user's usage history.
/// </summary>
public class ModelComparisonService(GeminiService gemini, UsageTrackingService usage)
{
    public const int MinModels = 2;

    public async Task<IReadOnlyList<ModelComparisonRow>> CompareAsync(
        string userId, string prompt, IReadOnlyList<string> models, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A signed-in user is required.", nameof(userId));

        var chosen = models
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (chosen.Count < MinModels)
            throw new ArgumentException($"Choose at least {MinModels} different models to compare.", nameof(models));

        var answered = new List<(string Model, GeminiReply Reply, ImpactEstimate Impact, EnvironmentalFactor Factor)>();
        var failed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var model in chosen)
        {
            try
            {
                var reply = await gemini.GenerateAsync(prompt, model, cancellationToken);
                var impact = TokenImpactEstimator.Estimate(reply.TotalTokens);
                var factor = TokenImpactEstimator.CreateFactor(reply.Model, reply.TotalTokens);
                answered.Add((reply.Model, reply, impact, factor));
            }
            catch (GeminiException ex)
            {
                failed[model] = ex.Message;
            }
        }

        // Rank with the Domain class from the UML (AIComparison): a higher efficiency score means a lower weighted impact.
        var scores = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (answered.Count > 0)
        {
            var comparison = new AIComparison { ModelList = answered.Select(item => item.Model).ToList() };
            foreach (var (name, score) in comparison.CompareModels(answered.Select(item => item.Factor)))
                scores[name] = (decimal)score;
        }

        var saveErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in answered)
        {
            try
            {
                await usage.RecordAsync(userId, queryCount: 1, item.Factor, cancellationToken);
            }
            catch (ArgumentException ex)
            {
                saveErrors[item.Model] = UserMessages.From(ex);
            }
            catch (DbUpdateException)
            {
                saveErrors[item.Model] = "Could not be saved. Please check the database connection.";
            }
        }

        var rows = new List<ModelComparisonRow>();
        var rank = 0;
        // Order by the exact estimated CO2 (the rounded efficiency score can tie for very small impacts).
        foreach (var item in answered.OrderBy(item => item.Impact.Co2Grams).ThenBy(item => item.Model, StringComparer.OrdinalIgnoreCase))
        {
            rows.Add(new ModelComparisonRow(item.Model, ++rank, item.Reply, item.Impact, scores[item.Model], null,
                saveErrors.GetValueOrDefault(item.Model)));
        }
        foreach (var model in chosen.Where(model => failed.ContainsKey(model)))
            rows.Add(new ModelComparisonRow(model, 0, null, null, null, failed[model], null));

        return rows;
    }
}
