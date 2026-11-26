using System.Net;
using AIEnvironmentalTracker.Web.Services;

namespace AIEnvironmentalTracker.Tests;

public class GeminiServiceTests
{
    private static FakeHandler Ok(int prompt = 5, int candidates = 6, int total = 11)
        => new(_ => (HttpStatusCode.OK, TestSupport.GeminiJson(prompt, candidates, total)));

    [Fact]
    public async Task Generate_SendsTheChosenModelAndKeepsTheKeyOutOfTheUrl()
    {
        var handler = Ok();
        var gemini = TestSupport.CreateGemini(handler);

        await gemini.GenerateAsync("hi", "model-b");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://example.test/v1beta/models/model-b:generateContent", request.RequestUri!.ToString());
        Assert.DoesNotContain("test-key", request.RequestUri.ToString());
        Assert.Equal("test-key", Assert.Single(request.Headers.GetValues("x-goog-api-key")));
    }

    [Fact]
    public async Task Generate_UsesTheDefaultModelWhenNoneIsChosen()
    {
        var handler = Ok();
        var reply = await TestSupport.CreateGemini(handler).GenerateAsync("hi");
        Assert.Equal("model-a", reply.Model);
        Assert.EndsWith("models/model-a:generateContent", handler.Requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task Generate_ReadsTheTokenCounts()
    {
        var reply = await TestSupport.CreateGemini(Ok(7, 8, 20)).GenerateAsync("hi");
        Assert.Equal(7, reply.PromptTokens);
        Assert.Equal(8, reply.CandidateTokens);
        Assert.Equal(20, reply.TotalTokens);
        Assert.Equal("hello", reply.Text);
    }

    [Fact]
    public async Task Generate_RefusesAModelThatIsNotInTheList_WithoutCallingGoogle()
    {
        var handler = Ok();
        var gemini = TestSupport.CreateGemini(handler);
        var ex = await Assert.ThrowsAsync<GeminiException>(() => gemini.GenerateAsync("hi", "gemini-expensive-pro"));
        Assert.Contains("not available", ex.Message);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Generate_RefusesATooLongPrompt_WithoutCallingGoogle()
    {
        var handler = Ok();
        var gemini = TestSupport.CreateGemini(handler, options => options.MaxPromptCharacters = 10);
        var ex = await Assert.ThrowsAsync<GeminiException>(() => gemini.GenerateAsync(new string('x', 11)));
        Assert.Contains("too long", ex.Message);
        Assert.Empty(handler.Requests);
        await gemini.GenerateAsync(new string('x', 10)); // exactly at the limit is fine
    }

    [Fact]
    public async Task Generate_RefusesAnEmptyPromptAndAMissingKey()
    {
        var handler = Ok();
        await Assert.ThrowsAsync<GeminiException>(() => TestSupport.CreateGemini(handler).GenerateAsync("   "));
        var noKey = TestSupport.CreateGemini(handler, options => options.ApiKey = null);
        var ex = await Assert.ThrowsAsync<GeminiException>(() => noKey.GenerateAsync("hi"));
        Assert.Equal(GeminiService.MissingKeyMessage, ex.Message);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "rate limit")]
    [InlineData(HttpStatusCode.NotFound, "not found")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "temporarily unavailable")]
    [InlineData(HttpStatusCode.Forbidden, "not allowed")]
    public async Task Generate_TurnsHttpErrorsIntoSafeMessages(HttpStatusCode status, string expected)
    {
        var gemini = TestSupport.CreateGemini(new FakeHandler(_ => (status, "{\"error\":{\"message\":\"internal detail\"}}")));
        var ex = await Assert.ThrowsAsync<GeminiException>(() => gemini.GenerateAsync("hi"));
        Assert.Contains(expected, ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal detail", ex.Message);
    }

    [Fact]
    public void AvailableModels_PutTheDefaultFirstAndDropDuplicatesAndBlanks()
    {
        var gemini = TestSupport.CreateGemini(Ok(), options =>
        {
            options.Model = "default";
            options.AllowedModels = ["other", "DEFAULT", " ", "other"];
        });
        Assert.Equal(["default", "other"], gemini.AvailableModels);
    }
}
