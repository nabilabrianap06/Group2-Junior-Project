namespace AIEnvironmentalTracker.Domain.Models;

public class RecommendationEngine
{
    private const double HighEnergyThreshold = 10;
    private const double HighWaterThreshold = 5;
    private const double HighCo2Threshold = 2;

    public int RecommendationId { get; set; }
    public string AdviceText { get; set; } = string.Empty;
    public string TargetMetric { get; set; } = string.Empty;

    public IReadOnlyList<RecommendationEngine> FetchRecommendations(CarbonFootprintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var recommendations = new List<RecommendationEngine>();

        if (report.TotalEnergyUsage >= HighEnergyThreshold)
        {
            recommendations.Add(CreateRecommendation(
                recommendationId: 1,
                adviceText: "Kurangi frekuensi query panjang dan gabungkan prompt agar penggunaan energi lebih efisien.",
                targetMetric: "Energy"));
        }

        if (report.TotalWaterUsage >= HighWaterThreshold)
        {
            recommendations.Add(CreateRecommendation(
                recommendationId: 2,
                adviceText: "Gunakan model yang lebih ringan untuk tugas sederhana agar konsumsi air pusat data lebih rendah.",
                targetMetric: "Water"));
        }

        if (report.TotalCO2Emission >= HighCo2Threshold)
        {
            recommendations.Add(CreateRecommendation(
                recommendationId: 3,
                adviceText: "Pilih model dengan jejak karbon lebih kecil untuk aktivitas rutin dan otomatisasi berulang.",
                targetMetric: "CO2"));
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(CreateRecommendation(
                recommendationId: 4,
                adviceText: "Pola penggunaan AI kamu sudah cukup efisien. Pertahankan dengan memilih model sesuai kebutuhan tugas.",
                targetMetric: "General"));
        }

        return recommendations;
    }

    private static RecommendationEngine CreateRecommendation(int recommendationId, string adviceText, string targetMetric)
    {
        return new RecommendationEngine
        {
            RecommendationId = recommendationId,
            AdviceText = adviceText,
            TargetMetric = targetMetric
        };
    }
}
