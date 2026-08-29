using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiEnvironmentalTracker.Models
{
    public class UpstreamRoute
    {
        public string Provider { get; set; } = "OpenAI";
        public string UpstreamUrl { get; set; } = string.Empty;
        public string? ResolvedApiKey { get; set; }
        public string Model { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    public class TelemetrySummaryDto
    {
        public int TotalRequests { get; set; }
        public int TotalTokens { get; set; }
        public double TotalEnergyKWh { get; set; }
        public double TotalCarbonGrams { get; set; }
        public double TotalWaterML { get; set; }
        public double AvgLatencyMs { get; set; }
        public double SmartphoneChargesEquivalent { get; set; }
        public double WaterBottlesEquivalent { get; set; }
        public double LedBulbHoursEquivalent { get; set; }
    }

    public class ProviderBreakdownDto
    {
        public string Provider { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public int TotalTokens { get; set; }
        public double EnergyKWh { get; set; }
        public double CarbonGrams { get; set; }
        public double WaterML { get; set; }
        public double Percentage { get; set; }
    }

    public class OpenAiModelListResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = "list";

        [JsonPropertyName("data")]
        public List<OpenAiModelItem> Data { get; set; } = new();
    }

    public class OpenAiModelItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = "model";

        [JsonPropertyName("created")]
        public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        [JsonPropertyName("owned_by")]
        public string OwnedBy { get; set; } = "ai-environmental-tracker";
    }
}
