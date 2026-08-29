using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiEnvironmentalTracker.Models
{
    /// <summary>
    /// Entity mapped to the "chat_logs" table in PostgreSQL.
    /// Each row captures one user-AI interaction together with
    /// the environmental telemetry computed from its token usage.
    /// </summary>
    [Table("chat_logs")]
    public class ChatLog
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("user_prompt")]
        public string UserPrompt { get; set; } = string.Empty;

        [Required]
        [Column("ai_response")]
        public string AIResponse { get; set; } = string.Empty;

        [Column("total_tokens")]
        public int TotalTokens { get; set; }

        [Column("energy_kwh")]
        public double EnergyKWh { get; set; }

        [Column("carbon_grams")]
        public double CarbonGrams { get; set; }

        [Column("water_ml")]
        public double WaterML { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
