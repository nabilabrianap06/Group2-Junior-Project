using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AiEnvironmentalTracker.Models;

namespace AiEnvironmentalTracker.Interfaces
{
    public interface IImpactRepository
    {
        // ── Proxy Telemetry Logs ─────────────────────────────────────────
        Task SaveUsageLogAsync(AiUsageLog log);
        Task<List<AiUsageLog>> GetRecentUsageLogsAsync(int count = 50);
        Task<TelemetrySummaryDto> GetTelemetrySummaryAsync();
        Task<List<ProviderBreakdownDto>> GetProviderBreakdownAsync();

        // ── Legacy Chat Logs (Backward compatibility) ───────────────────
        Task SaveLogAsync(ChatLog log);
        Task<List<ChatLog>> GetAllLogsAsync();
        Task<ChatLog?> GetLogByIdAsync(Guid id);
    }
}
