using AIEnvironmentalTracker.Web.Data;
using AIEnvironmentalTracker.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AIEnvironmentalTracker.Tests;

public class AdminServiceTests : IDisposable
{
    private const string Password = "Green#2026x";
    private readonly ServiceProvider provider = TestSupport.CreateIdentityServices();
    private readonly IServiceScope scope;
    private readonly AdminService admin;
    private readonly UserManager<ApplicationUser> users;

    public AdminServiceTests()
    {
        scope = provider.CreateScope();
        admin = scope.ServiceProvider.GetRequiredService<AdminService>();
        users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    public void Dispose()
    {
        scope.Dispose();
        provider.Dispose();
    }

    private async Task<ApplicationUser> UserAsync(string email, bool isAdmin = false)
    {
        Assert.True((await admin.AddUserAsync(email, Password, isAdmin)).Succeeded);
        return (await users.FindByEmailAsync(email))!;
    }

    [Fact]
    public async Task AddUser_CreatesAnAccount_AndCanMakeItAnAdmin()
    {
        var user = await UserAsync("a@example.test", isAdmin: true);
        Assert.True(await users.IsInRoleAsync(user, AdminService.AdminRole));
        Assert.False(await users.IsInRoleAsync(await UserAsync("b@example.test"), AdminService.AdminRole));
    }

    [Theory]
    [InlineData("not-an-email", Password)]
    [InlineData("c@example.test", "abc")]
    [InlineData("", Password)]
    [InlineData("c@example.test", "")]
    public async Task AddUser_RejectsBadInput(string email, string password)
        => Assert.False((await admin.AddUserAsync(email, password, false)).Succeeded);

    [Fact]
    public async Task AddUser_RejectsADuplicateEmail_IgnoringCase()
    {
        await UserAsync("dup@example.test");
        Assert.False((await admin.AddUserAsync("DUP@example.test", Password, false)).Succeeded);
    }

    [Fact]
    public async Task UpdateEmail_ChangesEmailAndSignInName_AndRefusesDuplicates()
    {
        var a = await UserAsync("a@example.test");
        await UserAsync("b@example.test");

        Assert.True((await admin.UpdateEmailAsync(a.Id, "renamed@example.test")).Succeeded);
        Assert.NotNull(await users.FindByNameAsync("renamed@example.test"));
        Assert.False((await admin.UpdateEmailAsync(a.Id, "b@example.test")).Succeeded);
        Assert.False((await admin.UpdateEmailAsync(a.Id, "nope")).Succeeded);
    }

    [Fact]
    public async Task Suspend_LocksTheAccount_AndReactivateUnlocksIt()
    {
        var boss = await UserAsync("boss@example.test", isAdmin: true);
        var user = await UserAsync("user@example.test");

        Assert.True((await admin.SetSuspendedAsync(boss.Id, user.Id, true)).Succeeded);
        Assert.True(await users.IsLockedOutAsync((await users.FindByIdAsync(user.Id))!));
        Assert.True((await admin.GetUsersAsync()).Single(u => u.Id == user.Id).IsSuspended);

        Assert.True((await admin.SetSuspendedAsync(boss.Id, user.Id, false)).Succeeded);
        Assert.False(await users.IsLockedOutAsync((await users.FindByIdAsync(user.Id))!));
    }

    [Fact]
    public async Task Suspend_RotatesTheSecurityStamp_SoOpenSessionsExpire()
    {
        var boss = await UserAsync("boss@example.test", isAdmin: true);
        var user = await UserAsync("user@example.test");
        var before = await users.GetSecurityStampAsync(user);
        await admin.SetSuspendedAsync(boss.Id, user.Id, true);
        Assert.NotEqual(before, await users.GetSecurityStampAsync((await users.FindByIdAsync(user.Id))!));
    }

    [Fact]
    public async Task AnAdmin_CannotSuspendOrDemoteThemselves()
    {
        var boss = await UserAsync("boss@example.test", isAdmin: true);
        await UserAsync("other@example.test", isAdmin: true);
        Assert.False((await admin.SetSuspendedAsync(boss.Id, boss.Id, true)).Succeeded);
        Assert.False((await admin.SetAdminAsync(boss.Id, boss.Id, false)).Succeeded);
    }

    [Fact]
    public async Task TheLastActiveAdmin_CannotBeSuspendedOrDemoted()
    {
        var boss = await UserAsync("boss@example.test", isAdmin: true);
        var user = await UserAsync("user@example.test");
        Assert.False((await admin.SetSuspendedAsync(user.Id, boss.Id, true)).Succeeded);
        Assert.False((await admin.SetAdminAsync(user.Id, boss.Id, false)).Succeeded);
        Assert.True(await users.IsInRoleAsync(boss, AdminService.AdminRole));
    }

    [Fact]
    public async Task ASuspendedAdmin_DoesNotCountAsActive()
    {
        var one = await UserAsync("one@example.test", isAdmin: true);
        var two = await UserAsync("two@example.test", isAdmin: true);

        // Two admins: suspending one of them is allowed because the other stays active.
        Assert.True((await admin.SetSuspendedAsync(one.Id, two.Id, true)).Succeeded);

        // Now "two" is suspended, so "one" is the only active admin and must be protected,
        // also when the request comes from a different account.
        var refused = await admin.SetSuspendedAsync("someone-else", one.Id, true);
        Assert.False(refused.Succeeded);
        Assert.Contains("last active admin", refused.Message);
        Assert.False((await admin.SetAdminAsync("someone-else", one.Id, false)).Succeeded);
    }

    [Fact]
    public async Task SetAdmin_GrantsAndRevokes_WhenAnotherAdminExists()
    {
        var boss = await UserAsync("boss@example.test", isAdmin: true);
        var user = await UserAsync("user@example.test");

        Assert.True((await admin.SetAdminAsync(boss.Id, user.Id, true)).Succeeded);
        Assert.True(await users.IsInRoleAsync(user, AdminService.AdminRole));
        Assert.True((await admin.SetAdminAsync(boss.Id, user.Id, false)).Succeeded);
        Assert.False(await users.IsInRoleAsync(user, AdminService.AdminRole));
    }

    [Fact]
    public async Task UnknownUser_IsReported()
    {
        Assert.False((await admin.SetSuspendedAsync("x", "missing", true)).Succeeded);
        Assert.False((await admin.SetAdminAsync("x", "missing", true)).Succeeded);
        Assert.False((await admin.UpdateEmailAsync("missing", "a@example.test")).Succeeded);
    }

    [Fact]
    public async Task PromoteConfiguredAdmin_PromotesOnlyTheMatchingEmail()
    {
        var match = await UserAsync("first@example.test");
        var other = await UserAsync("other@example.test");

        Assert.False(await admin.PromoteConfiguredAdminAsync(null));
        Assert.False(await admin.PromoteConfiguredAdminAsync("nobody@example.test"));
        Assert.True(await admin.PromoteConfiguredAdminAsync("FIRST@example.test"));
        Assert.True(await users.IsInRoleAsync(match, AdminService.AdminRole));
        Assert.False(await users.IsInRoleAsync(other, AdminService.AdminRole));
        Assert.False(await admin.PromoteConfiguredAdminAsync("first@example.test")); // already an admin
    }

    [Fact]
    public async Task PlatformTotals_SumAllUsersAndModels()
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var a = await UserAsync("a@example.test", isAdmin: true);
        var b = await UserAsync("b@example.test");
        db.UsageRecords.AddRange(
            new UsageRecord { ApplicationUserId = a.Id, AiModelName = "X", QueryCount = 2, TotalWater = 1, TotalCo2 = 2, TotalEnergy = 3 },
            new UsageRecord { ApplicationUserId = b.Id, AiModelName = "X", QueryCount = 3, TotalWater = 1, TotalCo2 = 2, TotalEnergy = 3 },
            new UsageRecord { ApplicationUserId = b.Id, AiModelName = "Y", QueryCount = 5, TotalWater = 10, TotalCo2 = 20, TotalEnergy = 30 });
        await db.SaveChangesAsync();

        var totals = await admin.GetPlatformTotalsAsync();
        Assert.Equal(2, totals.Users);
        Assert.Equal(1, totals.Admins);
        Assert.Equal(10, totals.Queries);
        Assert.Equal(12m, totals.Water);
        Assert.Equal(["Y", "X"], totals.Models.Select(m => m.ModelName));
        Assert.Equal(5, totals.Models.Single(m => m.ModelName == "X").Queries);
    }
}
