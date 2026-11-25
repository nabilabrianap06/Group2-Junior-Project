using AIEnvironmentalTracker.Domain.Models;

namespace AIEnvironmentalTracker.Tests;

public class TokenImpactEstimatorTests
{
    [Fact]
    public void ReferencePrompt_EnergyIsTheGooglePublishedFigure()
        => Assert.Equal(0.24m, TokenImpactEstimator.Estimate(500).EnergyWh);

    [Fact]
    public void ReferencePrompt_OverheadIsAboutTwoHundredthsOfAWattHour()
        => Assert.InRange(TokenImpactEstimator.Estimate(500).OverheadEnergyWh, 0.0195m, 0.0205m);

    [Fact]
    public void ReferencePrompt_CarbonMatchesThePaper()
    {
        var estimate = TokenImpactEstimator.Estimate(500);
        Assert.Equal(0.023m, Math.Round(estimate.OperationalCo2Grams, 3)); // Scope 2, market-based
        Assert.Equal(0.03m, Math.Round(estimate.Co2Grams, 2));            // published headline
    }

    [Fact]
    public void ReferencePrompt_WaterIsWithinThreePercentOfThePublishedFigure()
        => Assert.InRange(TokenImpactEstimator.Estimate(500).WaterMl, 0.26m * 0.97m, 0.26m * 1.03m);

    [Fact]
    public void Impact_ScalesLinearlyWithTokens()
    {
        var one = TokenImpactEstimator.Estimate(500);
        var two = TokenImpactEstimator.Estimate(1000);
        // decimal division keeps 28 digits, so compare with a tolerance far below any visible digit
        Assert.Equal(one.EnergyWh * 2, two.EnergyWh, 20);
        Assert.Equal(one.Co2Grams * 2, two.Co2Grams, 20);
        Assert.Equal(one.WaterMl * 2, two.WaterMl, 20);
    }

    [Fact]
    public void ZeroTokens_GiveZeroImpact()
    {
        var estimate = TokenImpactEstimator.Estimate(0);
        Assert.Equal(0m, estimate.EnergyWh);
        Assert.Equal(0m, estimate.Co2Grams);
        Assert.Equal(0m, estimate.WaterMl);
    }

    [Fact]
    public void LocationBasedFactor_RaisesOnlyTheOperationalCarbon()
    {
        var market = TokenImpactEstimator.Estimate(500);
        var location = TokenImpactEstimator.Estimate(500, ImpactParameters.GoogleLocationBased2025);
        Assert.Equal(345m / 94m, location.OperationalCo2Grams / market.OperationalCo2Grams, 6);
        Assert.Equal(market.WaterMl, location.WaterMl);
        Assert.Equal(market.EmbodiedCo2Grams, location.EmbodiedCo2Grams);
    }

    [Fact]
    public void NegativeTokens_AreRejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => TokenImpactEstimator.Estimate(-1));

    [Fact]
    public void PueBelowOne_IsRejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => TokenImpactEstimator.Estimate(1, ImpactParameters.Google2025 with { Pue = 0.9m }));

    [Fact]
    public void CreateFactor_CarriesTheEstimate()
    {
        var estimate = TokenImpactEstimator.Estimate(321);
        var factor = TokenImpactEstimator.CreateFactor("m", 321);
        Assert.Equal("m", factor.AiModelName);
        Assert.Equal(estimate.EnergyWh, factor.EnergyFactorPerQuery);
        Assert.Equal(estimate.Co2Grams, factor.Co2FactorPerQuery);
        Assert.Equal(estimate.WaterMl, factor.WaterFactorPerQuery);
    }
}
