using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PRC.Common;
using PRC.Common.Messages;
using PRC.IdentityService.Data;
using PRC.IdentityService.Models;

namespace PRC.IdentityService.Events;

public class GetIdentityStatsConsumer : IConsumer<GetIdentityStatsRequest>
{
    private readonly IdentityDbContext _db;
    public GetIdentityStatsConsumer(IdentityDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetIdentityStatsRequest> ctx)
    {
        var yearStart        = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var total            = await _db.Users.CountAsync();
        var pending          = await _db.Users.CountAsync(u => u.Role == UserRole.Pending);
        var active           = await _db.Users.CountAsync(u => u.IsActive && u.Role != UserRole.Pending);
        var fanciers         = await _db.Users.CountAsync(u => u.Role == UserRole.Fancier);
        var usersThisYear    = await _db.Users.CountAsync(u => u.CreatedAt >= yearStart);
        var fanciersThisYear = await _db.Users.CountAsync(u => u.Role == UserRole.Fancier && u.CreatedAt >= yearStart);
        await ctx.RespondAsync(new IdentityStatsResult(total, pending, active, fanciers, usersThisYear, fanciersThisYear));
    }
}

public class ValidateAdminCredentialsConsumer : IConsumer<ValidateAdminCredentialsRequest>
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public ValidateAdminCredentialsConsumer(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn)
    {
        _users  = users;
        _signIn = signIn;
    }

    public async Task Consume(ConsumeContext<ValidateAdminCredentialsRequest> ctx)
    {
        var user = await _users.FindByEmailAsync(ctx.Message.Email);
        if (user is null)
        {
            await ctx.RespondAsync(new ValidateAdminCredentialsResult(false, Guid.Empty, string.Empty, null, false));
            return;
        }

        var result = await _signIn.CheckPasswordSignInAsync(user, ctx.Message.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await ctx.RespondAsync(new ValidateAdminCredentialsResult(false, user.Id, user.FullName, user.Role, user.IsActive));
            return;
        }

        await ctx.RespondAsync(new ValidateAdminCredentialsResult(true, user.Id, user.FullName, user.Role, user.IsActive));
    }
}

public class GetUsersConsumer : IConsumer<GetUsersRequest>
{
    private readonly IdentityDbContext _db;
    public GetUsersConsumer(IdentityDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetUsersRequest> ctx)
    {
        var m = ctx.Message;
        var q = _db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(m.Search))
            q = q.Where(u => u.Email!.Contains(m.Search)
                           || u.FirstName.Contains(m.Search)
                           || u.LastName.Contains(m.Search));

        if (m.Role.HasValue)
            q = q.Where(u => u.Role == m.Role.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((m.Page - 1) * m.PageSize)
            .Take(m.PageSize)
            .Select(u => new AdminUserItem(
                u.Id, u.Email!, u.FirstName, u.LastName,
                u.Role, u.IsActive, u.FederationId, u.LastLoginAt))
            .ToListAsync();

        await ctx.RespondAsync(new GetUsersResult(items, total));
    }
}

public class ToggleUserActiveConsumer : IConsumer<ToggleUserActiveRequest>
{
    private readonly UserManager<ApplicationUser> _users;
    public ToggleUserActiveConsumer(UserManager<ApplicationUser> users) => _users = users;

    public async Task Consume(ConsumeContext<ToggleUserActiveRequest> ctx)
    {
        var user = await _users.FindByIdAsync(ctx.Message.UserId.ToString());
        if (user is null)
        {
            await ctx.RespondAsync(new ToggleUserActiveResult(ctx.Message.UserId, false, "User not found."));
            return;
        }

        user.IsActive  = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        await ctx.RespondAsync(new ToggleUserActiveResult(user.Id, user.IsActive, null));
    }
}

public class AssignRoleConsumer : IConsumer<AssignRoleRequest>
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPublishEndpoint _bus;

    public AssignRoleConsumer(UserManager<ApplicationUser> users, IPublishEndpoint bus)
    {
        _users = users;
        _bus   = bus;
    }

    public async Task Consume(ConsumeContext<AssignRoleRequest> ctx)
    {
        var m    = ctx.Message;
        var user = await _users.FindByIdAsync(m.UserId.ToString());
        if (user is null)
        {
            await ctx.RespondAsync(new AssignRoleResult(m.UserId, m.Role, m.FederationId, "User not found."));
            return;
        }

        var oldRole = user.Role.ToString();
        user.Role      = m.Role;
        user.FederationId = m.FederationId;
        user.UpdatedAt = DateTime.UtcNow;

        await _users.RemoveFromRoleAsync(user, oldRole);
        await _users.AddToRoleAsync(user, m.Role.ToString());
        await _users.UpdateAsync(user);

        if (m.Role == UserRole.FederationManager && m.FederationId.HasValue && !string.IsNullOrEmpty(user.Email))
        {
            await _bus.Publish(new FederationManagerAssigned(
                m.FederationId.Value, user.Email, user.FullName, DateTime.UtcNow), ctx.CancellationToken);
        }

        await ctx.RespondAsync(new AssignRoleResult(user.Id, user.Role, user.FederationId, null));
    }
}

public class SetUserLimitsConsumer : IConsumer<SetUserLimitsRequest>
{
    private readonly UserManager<ApplicationUser> _users;
    public SetUserLimitsConsumer(UserManager<ApplicationUser> users) => _users = users;

    public async Task Consume(ConsumeContext<SetUserLimitsRequest> ctx)
    {
        var m    = ctx.Message;
        var user = await _users.FindByIdAsync(m.UserId.ToString());
        if (user is null)
        {
            await ctx.RespondAsync(new SetUserLimitsResult(m.UserId, null, null, "User not found."));
            return;
        }

        user.MaxResultsOverride = m.MaxResults;
        user.MaxClubsOverride   = m.MaxClubs;
        user.UpdatedAt          = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        await ctx.RespondAsync(new SetUserLimitsResult(user.Id, user.MaxResultsOverride, user.MaxClubsOverride, null));
    }
}

public class GetUserNamesConsumer : IConsumer<GetUserNamesRequest>
{
    private readonly IdentityDbContext _db;
    public GetUserNamesConsumer(IdentityDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetUserNamesRequest> ctx)
    {
        var ids = ctx.Message.UserIds.Distinct().ToList();
        var users = await _db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync();

        var names = users.ToDictionary(
            u => u.Id,
            u => $"{u.FirstName} {u.LastName}".Trim());

        await ctx.RespondAsync(new UserNamesResult(names));
    }
}

public class GetUserEmailsConsumer : IConsumer<GetUserEmailsRequest>
{
    private readonly IdentityDbContext _db;
    public GetUserEmailsConsumer(IdentityDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<GetUserEmailsRequest> ctx)
    {
        var ids = ctx.Message.UserIds.Distinct().ToList();
        var users = await _db.Users
            .Where(u => ids.Contains(u.Id) && u.Email != null)
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();

        var emails = users.ToDictionary(u => u.Id, u => u.Email!);
        await ctx.RespondAsync(new UserEmailsResult(emails));
    }
}

public class DeleteUserConsumer : IConsumer<DeleteUserRequest>
{
    private readonly IdentityDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public DeleteUserConsumer(IdentityDbContext db, UserManager<ApplicationUser> users)
    {
        _db    = db;
        _users = users;
    }

    public async Task Consume(ConsumeContext<DeleteUserRequest> ctx)
    {
        var m    = ctx.Message;
        var user = await _users.FindByIdAsync(m.UserId.ToString());
        if (user is null)
        {
            await ctx.RespondAsync(new DeleteUserResult(false, "User not found."));
            return;
        }

        if (user.Id == m.RequestingUserId)
        {
            await ctx.RespondAsync(new DeleteUserResult(false, "Cannot delete your own account."));
            return;
        }

        await _db.UpgradeRequests
            .Where(r => r.UserId == m.UserId)
            .ExecuteDeleteAsync(ctx.CancellationToken);

        var result = await _users.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            await ctx.RespondAsync(new DeleteUserResult(false, error));
            return;
        }

        await ctx.RespondAsync(new DeleteUserResult(true, null));
    }
}
