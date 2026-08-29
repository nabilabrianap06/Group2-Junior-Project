using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AiEnvironmentalTracker.Data;
using AiEnvironmentalTracker.Interfaces;
using AiEnvironmentalTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AiEnvironmentalTracker.Repositories
{
    public class ImpactRepository : IImpactRepository
    {
        private readonly AppDbContext _db;

        public ImpactRepository(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task SaveUsageLogAsync(AiUsageLog log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            _db.AiUsageLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<List<AiUsageLog>> GetRecentUsageLogsAsync(int count = 50)
        {
            return await _db.AiUsageLogs
                            .AsNoTracking()
                            .OrderByDescending(l => l.CreatedAt)
                            .Take(count)
                            .ToListAsync();
        }

        public async Task<TelemetrySummaryDto> GetTelemetrySummaryAsync()
        {
            var logs = await _db.AiUsageLogs.AsNoTracking().ToListAsync();

            if (logs.Count == 0)
            {
                return new TelemetrySummaryDto();
            }

            int totalTokens = logs.Sum(l => l.TotalTokens);
            double totalEnergy = logs.Sum(l => l.EnergyKWh);
            double totalCarbon = logs.Sum(l => l.CarbonGrams);
            double totalWater = logs.Sum(l => l.WaterML);
            double avgLatency = logs.Average(l => l.LatencyMs);

            // Equivalencies:
            // Smartphone battery ≈ 0.015 kWh (15 Wh) -> charges
            double phoneCharges = totalEnergy / 0.015;
            // 600 mL water bottle -> bottles
            double waterBottles = totalWater / 600.0;
            // 10W LED bulb hours: 10W = 0.01 kWh/hour -> hours = totalEnergy / 0.01
            double ledHours = totalEnergy / 0.01;

            return new TelemetrySummaryDto
            {
                TotalRequests = logs.Count,
                TotalTokens = totalTokens,
                TotalEnergyKWh = Math.Round(totalEnergy, 8),
                TotalCarbonGrams = Math.Round(totalCarbon, 6),
                TotalWaterML = Math.Round(totalWater, 6),
                AvgLatencyMs = Math.Round(avgLatency, 2),
                SmartphoneChargesEquivalent = Math.Round(phoneCharges, 2),
                WaterBottlesEquivalent = Math.Round(waterBottles, 4),
                LedBulbHoursEquivalent = Math.Round(ledHours, 2)
            };
        }

        public async Task<List<ProviderBreakdownDto>> GetProviderBreakdownAsync()
        {
            var logs = await _db.AiUsageLogs.AsNoTracking().ToListAsync();
            if (logs.Count == 0) return new List<ProviderBreakdownDto>();

            int grandTotalTokens = logs.Sum(l => l.TotalTokens);

            var grouped = logs.GroupBy(l => l.Provider)
                              .Select(g => new ProviderBreakdownDto
                              {
                                  Provider = g.Key,
                                  RequestCount = g.Count(),
                                  TotalTokens = g.Sum(x => x.TotalTokens),
                                  EnergyKWh = Math.Round(g.Sum(x => x.EnergyKWh), 8),
                                  CarbonGrams = Math.Round(g.Sum(x => x.CarbonGrams), 6),
                                  WaterML = Math.Round(g.Sum(x => x.WaterML), 6),
                                  Percentage = grandTotalTokens > 0 
                                      ? Math.Round((double)g.Sum(x => x.TotalTokens) / grandTotalTokens * 100.0, 1) 
                                      : 0
                              })
                              .OrderByDescending(x => x.TotalTokens)
                              .ToList();

            return grouped;
        }

        // ── Legacy ChatLog Support ──────────────────────────────────────

        public async Task SaveLogAsync(ChatLog log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            _db.ChatLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ChatLog>> GetAllLogsAsync()
        {
            return await _db.ChatLogs
                            .AsNoTracking()
                            .OrderByDescending(l => l.CreatedAt)
                            .ToListAsync();
        }

        public async Task<ChatLog?> GetLogByIdAsync(Guid id)
        {
            return await _db.ChatLogs
                            .AsNoTracking()
                            .FirstOrDefaultAsync(l => l.Id == id);
        }
    }
}
