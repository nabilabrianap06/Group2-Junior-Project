using System.Net;
using AIEnvironmentalTracker.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace AIEnvironmentalTracker.Tests;

public class ModelComparisonServiceTests
{
    // model-a answers with 100 total tokens, model-b with 40, model-c is "unavailable".
    private static FakeHandler Handler() => new(request =>
    {
        var url = request.RequestUri!.ToString();
        if (url.Contains("model-a")) return (HttpStatusCode.OK, TestSupport.GeminiJson(20, 30, 100));
        if (url.Contains("model-b")) return (HttpStatusCode.OK, TestSupport.GeminiJson(10, 25, 40));
        return (HttpStatusCode.ServiceUnavailable, "{}");
    });

    private static async Task<(ModelComparisonService Service, Web.Data.ApplicationDbContext Db)> CreateAsync()
    {
        var db = TestSupport.CreateDb();
        await TestSupport.AddUserAsync(db, "u1");
        var gemini = TestSupport.CreateGemini(Handler(), options => options.AllowedModels = ["model-a", "model-b", "model-c"]);
        return (new ModelComparisonService(gemini, new UsageTrackingService(db)), db);
    }

    [Fact]
    public async Task RanksTheModelWithTheFewestTokensFirst()
    {
        var (service, _) = await CreateAsync();
        var rows = await service.CompareAsync("u1", "hi", ["model-a", "model-b"]);

        Assert.Equal(["model-b", "model-a"], rows.Select(r => r.Model));
        Assert.Equal([1, 2], rows.Select(r => r.Rank));
        Assert.True(rows[0].Impact!.Co2Grams < rows[1].Impact!.Co2Grams);
        Assert.True(rows[0].EfficiencyScore >= rows[1].EfficiencyScore);
    }

    [Fact]
    public async Task ThinkingTokens_AreTheTotalMinusPromptAndAnswer()
    {
        var (service, _) = await CreateAsync();
        var rows = await service.CompareAsync("u1", "hi", ["model-a", "model-b"]);
        Assert.Equal(5, rows.Single(r => r.Model == "model-b").ThinkingTokens);   // 40 - 10 - 25
        Assert.Equal(50, rows.Single(r => r.Model == "model-a").ThinkingTokens);  // 100 - 20 - 30
    }

    [Fact]
    public async Task EachAnswer_IsSavedAsUsage()
    {
        var (service, db) = await CreateAsync();
        await service.CompareAsync("u1", "hi", ["model-a", "model-b"]);

        var saved = await db.UsageRecords.OrderBy(r => r.AiModelName).ToListAsync();
        Assert.Equal(["model-a", "model-b"], saved.Select(r => r.AiModelName));
        Assert.All(saved, r => Assert.Equal("u1", r.ApplicationUserId));
        Assert.All(saved, r => Assert.Equal(1, r.QueryCount));
    }

    [Fact]
    public async Task AFailingModel_IsListedLastWithItsError_AndNotSaved()
    {
        var (service, db) = await CreateAsync();
        var rows = await service.CompareAsync("u1", "hi", ["model-c", "model-a", "model-b"]);

        var failed = rows[^1];
        Assert.Equal("model-c", failed.Model);
        Assert.Equal(0, failed.Rank);
        Assert.Null(failed.Reply);
        Assert.Contains("temporarily unavailable", failed.Error);
        Assert.Equal(2, await db.UsageRecords.CountAsync());
    }

    [Fact]
    public async Task NeedsAtLeastTwoDifferentModels()
    {
        var (service, db) = await CreateAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.CompareAsync("u1", "hi", ["model-a"]));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CompareAsync("u1", "hi", ["model-a", "MODEL-A", " "]));
        Assert.Equal(0, await db.UsageRecords.CountAsync());
    }

    [Fact]
    public async Task AModelOutsideTheAllowedList_IsReportedAsAnError()
    {
        var (service, _) = await CreateAsync();
        var rows = await service.CompareAsync("u1", "hi", ["model-a", "not-allowed"]);
        Assert.Contains("not available", rows.Single(r => r.Model == "not-allowed").Error);
        Assert.Single(rows, r => r.Reply is not null);
    }
}
