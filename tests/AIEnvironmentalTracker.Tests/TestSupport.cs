using System.Net;
using System.Text;
using AIEnvironmentalTracker.Web.Data;
using AIEnvironmentalTracker.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AIEnvironmentalTracker.Tests;

/// <summary>Builds services on an in-memory database, so tests need no PostgreSQL.</summary>
internal static class TestSupport
{
    public static ServiceProvider CreateIdentityServices()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<AdminService>();
        return services.BuildServiceProvider();
    }

    public static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    public static async Task<ApplicationUser> AddUserAsync(ApplicationDbContext db, string name)
    {
        var user = new ApplicationUser { Id = name, UserName = name, Email = name + "@example.test" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static GeminiService CreateGemini(HttpMessageHandler handler, Action<GeminiOptions>? configure = null)
    {
        var options = new GeminiOptions
        {
            ApiKey = "test-key",
            Model = "model-a",
            AllowedModels = ["model-a", "model-b"],
            BaseUrl = "https://example.test/v1beta/"
        };
        configure?.Invoke(options);
        return new GeminiService(new HttpClient(handler), Options.Create(options), NullLogger<GeminiService>.Instance);
    }

    public static string GeminiJson(int prompt, int candidates, int total, string text = "hello")
        => $$"""
        {"candidates":[{"content":{"role":"model","parts":[{"text":"{{text}}"}]},"finishReason":"STOP"}],
         "usageMetadata":{"promptTokenCount":{{prompt}},"candidatesTokenCount":{{candidates}},"totalTokenCount":{{total}}},
         "modelVersion":"ignored"}
        """;
}

/// <summary>Answers each request with a function of the request, and remembers what was sent.</summary>
internal sealed class FakeHandler(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var (status, body) = respond(request);
        return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}

/// <summary>A clock the test moves by hand.</summary>
internal sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset now = start;
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan by) => now += by;
}
