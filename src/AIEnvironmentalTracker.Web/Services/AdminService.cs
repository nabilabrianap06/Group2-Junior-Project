using System.ComponentModel.DataAnnotations;
using AIEnvironmentalTracker.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIEnvironmentalTracker.Web.Services;

/// <summary>One account in the admin user list, with its own usage totals.</summary>
public sealed record AdminUserRow(
    string Id, string Email, bool IsAdmin, bool IsSuspended,
    int Queries, decimal Water, decimal Co2, decimal Energy, DateTime? LastUsedUtc);

/// <summary>Platform-wide totals over every user.</summary>
public sealed record PlatformTotals(
    int Users, int Admins, int Suspended,
    int Queries, decimal Water, decimal Co2, decimal Energy,
    IReadOnlyList<ModelBreakdown> Models);

/// <summary>Outcome of an admin action, with a message that is safe to show.</summary>
public sealed record AdminResult(bool Succeeded, string Message)
{
    public static AdminResult Ok(string message) => new(true, message);
    public static AdminResult Fail(string message) => new(false, message);
}

/// <summary>
/// Admin operations on top of ASP.NET Core Identity: list/add/edit/suspend accounts, grant the Admin
/// role, and aggregate usage across the platform. Accounts are suspended (locked out), never deleted,
/// so their usage history is kept.
/// </summary>
public class AdminService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    public const string AdminRole = "Admin";
    public const string AdminEmailSetting = "Admin:Email";

    /// <summary>Creates the Admin role if it does not exist yet.</summary>
    public async Task EnsureRoleAsync()
    {
        if (!await roleManager.RoleExistsAsync(AdminRole))
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
    }

    /// <summary>
    /// Makes the account with the configured email ("Admin:Email") an admin. Called at startup and after a
    /// registration, so the first admin needs no manual database edit. Returns true if the user was promoted.
    /// </summary>
    public async Task<bool> PromoteConfiguredAdminAsync(string? configuredEmail, ApplicationUser? user = null)
    {
        if (string.IsNullOrWhiteSpace(configuredEmail)) return false;

        user ??= await userManager.FindByEmailAsync(configuredEmail.Trim());
        if (user is null || !string.Equals(user.Email, configuredEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        await EnsureRoleAsync();
        if (await userManager.IsInRoleAsync(user, AdminRole)) return false;
        return (await userManager.AddToRoleAsync(user, AdminRole)).Succeeded;
    }

    public async Task<IReadOnlyList<AdminUserRow>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var usage = await db.UsageRecords.AsNoTracking()
            .GroupBy(record => record.ApplicationUserId)
            .Select(group => new
            {
                UserId = group.Key,
                Queries = group.Sum(record => record.QueryCount),
                Water = group.Sum(record => record.TotalWater),
                Co2 = group.Sum(record => record.TotalCo2),
                Energy = group.Sum(record => record.TotalEnergy),
                Last = group.Max(record => record.Timestamp)
            })
            .ToDictionaryAsync(row => row.UserId, cancellationToken);

        var adminIds = (await userManager.GetUsersInRoleAsync(AdminRole)).Select(user => user.Id).ToHashSet();
        var users = await db.Users.AsNoTracking().OrderBy(user => user.Email).ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        return users.Select(user =>
        {
            usage.TryGetValue(user.Id, out var u);
            return new AdminUserRow(
                user.Id, user.Email ?? user.UserName ?? user.Id,
                adminIds.Contains(user.Id),
                user.LockoutEnd is { } end && end > now,
                u?.Queries ?? 0, u?.Water ?? 0, u?.Co2 ?? 0, u?.Energy ?? 0,
                u?.Last);
        }).ToList();
    }

    public async Task<PlatformTotals> GetPlatformTotalsAsync(CancellationToken cancellationToken = default)
    {
        var users = await GetUsersAsync(cancellationToken);

        var models = await db.UsageRecords.AsNoTracking()
            .GroupBy(record => record.AiModelName)
            .Select(group => new ModelBreakdown(group.Key, group.Sum(record => record.QueryCount),
                group.Sum(record => record.TotalWater), group.Sum(record => record.TotalCo2),
                group.Sum(record => record.TotalEnergy)))
            .ToListAsync(cancellationToken);

        return new PlatformTotals(
            users.Count, users.Count(user => user.IsAdmin), users.Count(user => user.IsSuspended),
            models.Sum(model => model.Queries), models.Sum(model => model.Water),
            models.Sum(model => model.Co2), models.Sum(model => model.Energy),
            models.OrderByDescending(model => model.Co2).ThenBy(model => model.ModelName).ToList());
    }

    public async Task<AdminResult> AddUserAsync(string? email, string? password, bool makeAdmin)
    {
        email = email?.Trim();
        if (!IsValidEmail(email))
            return AdminResult.Fail("A valid email address is required.");
        if (string.IsNullOrEmpty(password))
            return AdminResult.Fail("A password is required.");
        if (await userManager.FindByEmailAsync(email!) is not null)
            return AdminResult.Fail("An account with this email already exists.");

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
            return AdminResult.Fail(Describe(created));

        if (makeAdmin)
        {
            await EnsureRoleAsync();
            await userManager.AddToRoleAsync(user, AdminRole);
        }
        return AdminResult.Ok($"Account {email} created.");
    }

    public async Task<AdminResult> UpdateEmailAsync(string userId, string? newEmail)
    {
        newEmail = newEmail?.Trim();
        if (!IsValidEmail(newEmail))
            return AdminResult.Fail("A valid email address is required.");

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return AdminResult.Fail("User not found.");

        var existing = await userManager.FindByEmailAsync(newEmail!);
        if (existing is not null && existing.Id != user.Id)
            return AdminResult.Fail("Another account already uses this email.");

        // The sign-in name is the email in this app, so both are changed together.
        var setEmail = await userManager.SetEmailAsync(user, newEmail);
        if (!setEmail.Succeeded) return AdminResult.Fail(Describe(setEmail));
        var setName = await userManager.SetUserNameAsync(user, newEmail);
        if (!setName.Succeeded) return AdminResult.Fail(Describe(setName));

        return AdminResult.Ok($"Email changed to {newEmail}.");
    }

    public async Task<AdminResult> SetSuspendedAsync(string actorId, string userId, bool suspended)
    {
        if (suspended && actorId == userId)
            return AdminResult.Fail("You cannot suspend your own account.");

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return AdminResult.Fail("User not found.");

        if (suspended && await IsLastActiveAdminAsync(user))
            return AdminResult.Fail("You cannot suspend the last active admin.");

        await userManager.SetLockoutEnabledAsync(user, true);
        if (suspended)
        {
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            // Invalidates cookies, so a signed-in user is signed out at the next revalidation.
            await userManager.UpdateSecurityStampAsync(user);
            return AdminResult.Ok($"{user.Email} is suspended and cannot sign in.");
        }

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        return AdminResult.Ok($"{user.Email} is active again.");
    }

    public async Task<AdminResult> SetAdminAsync(string actorId, string userId, bool isAdmin)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return AdminResult.Fail("User not found.");

        await EnsureRoleAsync();
        var already = await userManager.IsInRoleAsync(user, AdminRole);
        if (isAdmin == already)
            return AdminResult.Ok(isAdmin ? "Already an admin." : "Not an admin.");

        if (!isAdmin)
        {
            if (actorId == userId)
                return AdminResult.Fail("You cannot remove your own admin role.");
            if (await IsLastActiveAdminAsync(user))
                return AdminResult.Fail("You cannot remove the last active admin.");
        }

        var result = isAdmin
            ? await userManager.AddToRoleAsync(user, AdminRole)
            : await userManager.RemoveFromRoleAsync(user, AdminRole);
        if (!result.Succeeded) return AdminResult.Fail(Describe(result));

        // Role claims live in the cookie, so refresh it: the change applies at the user's next sign-in.
        await userManager.UpdateSecurityStampAsync(user);
        return AdminResult.Ok(isAdmin ? $"{user.Email} is now an admin." : $"{user.Email} is no longer an admin.");
    }

    // True if this user is the only admin who is not suspended.
    private async Task<bool> IsLastActiveAdminAsync(ApplicationUser user)
    {
        if (!await userManager.IsInRoleAsync(user, AdminRole)) return false;

        var now = DateTimeOffset.UtcNow;
        var others = (await userManager.GetUsersInRoleAsync(AdminRole))
            .Where(admin => admin.Id != user.Id && !(admin.LockoutEnd is { } end && end > now));
        return !others.Any();
    }

    private static bool IsValidEmail(string? email)
        => !string.IsNullOrWhiteSpace(email) && email.Length <= 256 && new EmailAddressAttribute().IsValid(email);

    private static string Describe(IdentityResult result)
        => string.Join(" ", result.Errors.Select(error => error.Description));
}
