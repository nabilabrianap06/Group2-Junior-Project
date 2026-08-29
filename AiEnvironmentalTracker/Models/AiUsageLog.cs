using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiEnvironmentalTracker.Models
{
    /// <summary>
    /// Entity mapped to "ai_usage_logs" table in PostgreSQL (Supabase).
    /// Stores real-time telemetry captured by the AI Proxy Gateway:
    /// provider, model, token usage, latency, and computed environmental footprint.
    /// </summary>
    [Table("ai_usage_logs")]
    public class AiUsageLog
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(64)]
        [Column("provider")]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        [Column("model_name")]
        public string ModelName { get; set; } = string.Empty;

        [Column("prompt_tokens")]
        public int PromptTokens { get; set; }

        [Column("completion_tokens")]
        public int CompletionTokens { get; set; }

        [Column("total_tokens")]
        public int TotalTokens { get; set; }

        [Column("energy_kwh")]
        public double EnergyKWh { get; set; }

        [Column("carbon_grams")]
        public double CarbonGrams { get; set; }

        [Column("water_ml")]
        public double WaterML { get; set; }

        [MaxLength(256)]
        [Column("analogy_string")]
        public string AnalogyString { get; set; } = string.Empty;

        [Column("latency_ms")]
        public long LatencyMs { get; set; }

        [Column("is_streaming")]
        public bool IsStreaming { get; set; }

        [Column("status_code")]
        public int StatusCode { get; set; } = 200;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
