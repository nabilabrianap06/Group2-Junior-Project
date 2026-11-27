using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using AIEnvironmentalTracker.Web.Components;
using AIEnvironmentalTracker.Web.Components.Account;
using AIEnvironmentalTracker.Web.Data;
using AIEnvironmentalTracker.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Installed (published) app: settings are read from oxygenai.settings.json next to the .exe,
// so installing needs no environment variables. The file is created from
// oxygenai.settings.example.json during setup and is never committed (see .gitignore).
// It goes right after the other JSON files, so environment variables and command-line arguments still win.
var settingsFile = new JsonConfigurationSource
{
    FileProvider = new PhysicalFileProvider(AppContext.BaseDirectory),
    Path = "oxygenai.settings.json",
    Optional = true,
    ReloadOnChange = false
};
var lastJsonSource = builder.Configuration.Sources.ToList().FindLastIndex(source => source is JsonConfigurationSource);
builder.Configuration.Sources.Insert(lastJsonSource + 1, settingsFile);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// The connection string comes from user-secrets (Development) or the environment variable
// ConnectionStrings__DefaultConnection. It is never stored in appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The PostgreSQL connection string is not set. Set 'ConnectionStrings:DefaultConnection' with " +
        "dotnet user-secrets, set the environment variable ConnectionStrings__DefaultConnection, or (installed app) " +
        "fill in oxygenai.settings.json next to OxyGenAI.exe. See README.md.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<UsageTrackingService>();
builder.Services.AddScoped<BrowserTimeService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ModelComparisonService>();

// Per-user allowance for Gemini requests (section "AskLimits"); counters are kept in memory.
builder.Services.Configure<AskLimitOptions>(builder.Configuration.GetSection(AskLimitOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AskLimiter>();

// Gemini web API: key from user-secrets "Gemini:ApiKey" or environment variable Gemini__ApiKey.
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddHttpClient<GeminiService>(client => client.Timeout = TimeSpan.FromSeconds(60));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // No real email sender is configured, so accounts can sign in right after registering.
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Create or update the database schema on startup, so setup does not need `dotnet ef`.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex,
            "Could not apply database migrations. Check the internet connection and that the Neon database in the " +
            "connection string exists, and the username and password are correct.");
        throw;
    }
}

// Make sure the Admin role and the admin account exist (default admin@email.com, see AdminService).
// "Admin:Email" and "Admin:Password" (user-secrets, environment variables or oxygenai.settings.json) replace the defaults.
using (var scope = app.Services.CreateScope())
{
    var admin = scope.ServiceProvider.GetRequiredService<AdminService>();
    var seeded = await admin.EnsureDefaultAdminAsync(
        app.Configuration[AdminService.AdminEmailSetting],
        app.Configuration[AdminService.AdminPasswordSetting]);
    if (seeded != AdminSeedResult.Unchanged)
        app.Logger.LogInformation("Admin account {Result}.", seeded);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Installed app: open the browser on the app once it is listening ("OpenBrowser": true).
if (app.Configuration.GetValue<bool>("OpenBrowser"))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = app.Urls.FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                  ?? app.Urls.FirstOrDefault();
        if (url is null) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Could not open the browser. Open {Url} yourself.", url);
        }
    });
}

app.Run();
