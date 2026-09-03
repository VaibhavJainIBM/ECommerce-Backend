using System.Data;
using System.Net.Mail;
using ECommerce.Application.Common;
using ECommerce.Application.SellerTeams;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class SellerTeamRepository(ECommerceDbContext db) : ISellerTeamRepository
{
    public async Task<Result<IReadOnlyList<SellerMemberResponseDto>>> GetMembersAsync(Guid actor, Guid sellerId, CancellationToken ct)
    {
        if (!await IsOwnerAsync(actor, sellerId, ct))
            return Result<IReadOnlyList<SellerMemberResponseDto>>.Failure(SellerTeamErrors.NotFound);
        var members = await Members(sellerId).OrderBy(m => m.InvitedAtUtc).ToListAsync(ct);
        var result = new List<SellerMemberResponseDto>();
        foreach (var member in members) result.Add(await MapAsync(member, ct));
        return Result<IReadOnlyList<SellerMemberResponseDto>>.Success(result);
    }

    public async Task<Result<SellerMemberResponseDto>> GetMemberAsync(Guid actor, Guid sellerId, Guid memberId, CancellationToken ct)
    {
        if (!await IsOwnerAsync(actor, sellerId, ct)) return Missing();
        var member = await Members(sellerId).SingleOrDefaultAsync(m => m.Id == memberId, ct);
        return member is null ? Missing() : Result<SellerMemberResponseDto>.Success(await MapAsync(member, ct));
    }

    public Task<Result<SellerMemberResponseDto>> InviteAsync(Guid actor, Guid sellerId, InviteSellerMemberRequestDto? request, CancellationToken ct)
    {
        var role = SellerRoleNames.Canonical(request?.Role);
        if (request is null || !MailAddress.TryCreate(request.Email?.Trim(), out var email) ||
            !string.Equals(email.Address, request.Email?.Trim(), StringComparison.OrdinalIgnoreCase) || email.Address.Length > 256 || role is null)
            return Task.FromResult(Invalid("Provide a registered user's email and role Owner, Manager, or WarehouseStaff."));

        return MutateAsync(actor, sellerId, true, async () =>
        {
            var normalizedEmail = email.Address.ToUpperInvariant();
            var user = await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && u.IsActive, ct);
            if (user is null) return Invalid("An active registered account with that email is required.");
            var member = await Members(sellerId).SingleOrDefaultAsync(m => m.UserId == user.Id, ct);
            if (member is not null && member.Status != SellerMemberStatus.Removed)
                return Conflict("This user already has a membership or invitation.");
            if (member is null) { member = new SellerMember(sellerId, user.Id); db.SellerMembers.Add(member); }
            else member.Reinvite();
            var roles = await EnsureRolesAsync(sellerId, ct);
            var selected = roles.Single(r => r.Name == role);
            var existing = member.RoleAssignments.SingleOrDefault(a => a.SellerRoleId == selected.Id);
            if (existing is null) db.SellerMemberRoles.Add(new SellerMemberRole(member, selected));
            else existing.Restore();
            await db.SaveChangesAsync(ct);
            return Result<SellerMemberResponseDto>.Success(await MapAsync(member, ct));
        }, ct);
    }

    public Task<Result<SellerMemberResponseDto>> AcceptAsync(Guid actor, Guid sellerId, CancellationToken ct) =>
        MutateAsync(actor, sellerId, false, async () =>
        {
            var member = await Members(sellerId).SingleOrDefaultAsync(m => m.UserId == actor, ct);
            if (member is null) return Missing();
            if (member.Status == SellerMemberStatus.Active)
                return Result<SellerMemberResponseDto>.Success(await MapAsync(member, ct));
            if (member.Status != SellerMemberStatus.Invited)
                return Conflict("Only a pending invitation can be accepted.");
            member.Activate();
            await db.SaveChangesAsync(ct);
            return Result<SellerMemberResponseDto>.Success(await MapAsync(member, ct));
        }, ct);

    public async Task<Result<IReadOnlyList<SellerInvitationResponseDto>>> GetInvitationsAsync(Guid actor, CancellationToken ct)
    {
        if (!await IsActiveUserAsync(actor, ct))
            return Result<IReadOnlyList<SellerInvitationResponseDto>>.Failure(SellerTeamErrors.Unauthorized);
        var invitations = await db.SellerMembers.AsNoTracking().Include(m => m.Seller)
            .Include(m => m.RoleAssignments).ThenInclude(a => a.SellerRole)
            .Where(m => m.UserId == actor && m.Status == SellerMemberStatus.Invited)
            .OrderBy(m => m.InvitedAtUtc).ToListAsync(ct);
        return Result<IReadOnlyList<SellerInvitationResponseDto>>.Success(invitations.Select(m =>
            new SellerInvitationResponseDto(m.SellerId, m.Seller.DisplayName, m.Id, ActiveRoles(m))).ToArray());
    }

    public Task<Result<SellerMemberResponseDto>> ChangeStateAsync(Guid actor, Guid sellerId, Guid memberId, string action, CancellationToken ct) =>
        MutateAsync(actor, sellerId, true, async () =>
        {
            var member = await Members(sellerId).SingleOrDefaultAsync(m => m.Id == memberId, ct);
            if (member is null) return Missing();
            if (action is not ("suspend" or "reactivate" or "remove")) return Invalid("Unknown membership action.");
            if (action is "suspend" or "remove" && await IsLastOwnerAsync(member, ct))
                return Conflict("A seller must retain at least one active Owner.");
            if (action == "suspend")
            {
                if (member.Status != SellerMemberStatus.Active) return Conflict("Only active members can be suspended.");
                member.Suspend();
            }
            else if (action == "reactivate")
            {
                if (member.Status != SellerMemberStatus.Suspended) return Conflict("Only suspended members can be reactivated.");
                member.Reactivate();
            }
            else
            {
                member.Remove();
                foreach (var assignment in member.RoleAssignments) assignment.Revoke();
                foreach (var assignment in member.WarehouseAssignments) assignment.Remove();
            }
            await db.SaveChangesAsync(ct);
            return Result<SellerMemberResponseDto>.Success(await MapAsync(member, ct));
        }, ct);

    public Task<Result<SellerMemberResponseDto>> SetRoleAsync(Guid actor, Guid sellerId, Guid memberId, string role, bool assigned, CancellationToken ct)
    {
        var canonical = SellerRoleNames.Canonical(role);
        if (canonical is null) return Task.FromResult(Invalid("Role must be Owner, Manager, or WarehouseStaff."));
        return MutateAsync(actor, sellerId, true, async () =>
        {
            var member = await Members(sellerId).SingleOrDefaultAsync(m => m.Id == memberId, ct);
            if (member is null) return Missing();
            if (member.Status == SellerMemberStatus.Removed) return Conflict("Reinvite a removed member before assigning roles.");
            if (!assigned && canonical == SellerRoleNames.Owner && await IsLastOwnerAsync(member, ct))
                return Conflict("The last active Owner role cannot be revoked.");
            var roles = await EnsureRolesAsync(sellerId, ct);
            var selected = roles.Single(r => r.Name == canonical);
            var assignment = member.RoleAssignments.SingleOrDefault(a => a.SellerRoleId == selected.Id);
            if (assigned)
            {
                if (assignment is null) db.SellerMemberRoles.Add(new SellerMemberRole(member, selected));
                else assignment.Restore();
            }
            else assignment?.Revoke();
            await db.SaveChangesAsync(ct);
            return Result<SellerMemberResponseDto>.Success(await MapAsync(member, ct));
        }, ct);
    }

    public Task<Result<SellerMemberResponseDto>> SetWarehouseAsync(Guid actor, Guid sellerId, Guid memberId, Guid warehouseId, bool assigned, CancellationToken ct) =>
        MutateAsync(actor, sellerId, true, async () =>
        {
            var member = await Members(sellerId).SingleOrDefaultAsync(m => m.Id == memberId, ct);
            var warehouse = await db.Warehouses.SingleOrDefaultAsync(w => w.SellerId == sellerId && w.Id == warehouseId, ct);
            if (member is null || warehouse is null) return Missing();
            if (member.Status == SellerMemberStatus.Removed) return Conflict("Removed members cannot receive warehouse assignments.");
            var assignment = member.WarehouseAssignments.SingleOrDefault(a => a.WarehouseId == warehouseId);
            if (assigned)
            {
                if (assignment is null) db.WarehouseAssignments.Add(new WarehouseAssignment(member, warehouse));
                else assignment.Restore();
            }
            else assignment?.Remove();
            await db.SaveChangesAsync(ct);
            return Result<SellerMemberResponseDto>.Success(await MapAsync(member, ct));
        }, ct);

    private IQueryable<SellerMember> Members(Guid sellerId) => db.SellerMembers
        .Include(m => m.RoleAssignments).ThenInclude(a => a.SellerRole)
        .Include(m => m.WarehouseAssignments).Where(m => m.SellerId == sellerId);

    private Task<bool> IsActiveUserAsync(Guid actor, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.Id == actor && u.IsActive, ct);

    private async Task<bool> IsOwnerAsync(Guid actor, Guid sellerId, CancellationToken ct) =>
        await IsActiveUserAsync(actor, ct) && await db.SellerMembers.AnyAsync(m =>
            m.SellerId == sellerId && m.UserId == actor && m.Status == SellerMemberStatus.Active &&
            m.RoleAssignments.Any(a => a.SellerId == sellerId && a.IsActive && a.SellerRole.IsActive &&
                a.SellerRole.NormalizedName == "OWNER"), ct);

    private async Task<bool> IsLastOwnerAsync(SellerMember member, CancellationToken ct)
    {
        if (member.Status != SellerMemberStatus.Active || !ActiveRoles(member).Contains(SellerRoleNames.Owner))
            return false;
        return !await db.SellerMembers.AnyAsync(m => m.SellerId == member.SellerId && m.Id != member.Id &&
            m.Status == SellerMemberStatus.Active && db.Users.Any(u => u.Id == m.UserId && u.IsActive) &&
            m.RoleAssignments.Any(a => a.IsActive && a.SellerRole.IsActive && a.SellerRole.NormalizedName == "OWNER"), ct);
    }

    private async Task<List<SellerRole>> EnsureRolesAsync(Guid sellerId, CancellationToken ct)
    {
        var roles = await db.SellerRoles.Where(r => r.SellerId == sellerId).ToListAsync(ct);
        foreach (var name in new[] { SellerRoleNames.Owner, SellerRoleNames.Manager, SellerRoleNames.WarehouseStaff })
        {
            var role = roles.SingleOrDefault(r => r.NormalizedName == name.ToUpperInvariant());
            if (role is null)
            {
                role = new SellerRole(sellerId, name, "Built-in seller " + name + " role", true);
                db.SellerRoles.Add(role); roles.Add(role);
            }
            else role.Reactivate();
        }
        return roles;
    }

    private static IReadOnlyList<string> ActiveRoles(SellerMember member) => member.RoleAssignments
        .Where(a => a.IsActive && a.SellerRole.IsActive).Select(a => a.SellerRole.Name).OrderBy(x => x).ToArray();

    private async Task<SellerMemberResponseDto> MapAsync(SellerMember member, CancellationToken ct)
    {
        var email = await db.Users.Where(u => u.Id == member.UserId).Select(u => u.Email).SingleAsync(ct) ?? "";
        return new(member.Id, member.SellerId, member.UserId, email, member.Status.ToString(), ActiveRoles(member),
            member.WarehouseAssignments.Where(a => a.IsActive).Select(a => a.WarehouseId).ToArray(), member.InvitedAtUtc, member.JoinedAtUtc);
    }

    private async Task<Result<SellerMemberResponseDto>> MutateAsync(Guid actor, Guid sellerId, bool ownerOnly,
        Func<Task<Result<SellerMemberResponseDto>>> action, CancellationToken ct)
    {
        if (sellerId == Guid.Empty || actor == Guid.Empty) return Missing();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var output = new SqlParameter("@lockResult", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = "ecommerce:seller-team:" + sellerId.ToString("N") };
            await db.Database.ExecuteSqlRawAsync(
                "EXEC @lockResult = sys.sp_getapplock @Resource=@resource, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=5000;",
                [output, resource], ct);
            if (output.Value is not int status || status < 0)
                return Conflict("Seller team is being changed. Retry the request.");
            if (ownerOnly ? !await IsOwnerAsync(actor, sellerId, ct) : !await IsActiveUserAsync(actor, ct))
                return Missing();
            var result = await action();
            if (result.IsSuccess) await tx.CommitAsync(ct);
            else { await tx.RollbackAsync(ct); db.ChangeTracker.Clear(); }
            return result;
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException ||
            ex is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 or 1205 or 1222 } } ||
            ex is SqlException { Number: 1205 or 1222 })
        {
            try { await tx.RollbackAsync(CancellationToken.None); } catch (InvalidOperationException) { }
            db.ChangeTracker.Clear();
            return Conflict("The team changed concurrently. Refresh and retry.");
        }
    }

    private static Result<SellerMemberResponseDto> Missing() => Result<SellerMemberResponseDto>.Failure(SellerTeamErrors.NotFound);
    private static Result<SellerMemberResponseDto> Invalid(string message) => Result<SellerMemberResponseDto>.Failure(SellerTeamErrors.Validation(message));
    private static Result<SellerMemberResponseDto> Conflict(string message) => Result<SellerMemberResponseDto>.Failure(SellerTeamErrors.Conflict(message));
}

