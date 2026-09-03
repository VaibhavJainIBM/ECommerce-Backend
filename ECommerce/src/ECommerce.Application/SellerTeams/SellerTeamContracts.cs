using ECommerce.Application.Common;
namespace ECommerce.Application.SellerTeams;

public sealed record InviteSellerMemberRequestDto(string? Email, string? Role);
public sealed record SellerRoleResponseDto(string Name, string Description);
public sealed record SellerMemberResponseDto(Guid MemberId, Guid SellerId, Guid UserId, string Email,
    string Status, IReadOnlyList<string> Roles, IReadOnlyList<Guid> WarehouseIds, DateTimeOffset InvitedAtUtc, DateTimeOffset? JoinedAtUtc);
public sealed record SellerInvitationResponseDto(Guid SellerId, string SellerName, Guid MemberId, IReadOnlyList<string> Roles);

public interface ISellerTeamService
{
    Task<Result<IReadOnlyList<SellerMemberResponseDto>>> GetMembersAsync(Guid sellerId, CancellationToken ct = default);
    Task<Result<SellerMemberResponseDto>> GetMemberAsync(Guid sellerId, Guid memberId, CancellationToken ct = default);
    Task<Result<SellerMemberResponseDto>> InviteAsync(Guid sellerId, InviteSellerMemberRequestDto? request, CancellationToken ct = default);
    Task<Result<SellerMemberResponseDto>> AcceptAsync(Guid sellerId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SellerInvitationResponseDto>>> GetInvitationsAsync(CancellationToken ct = default);
    Task<Result<SellerMemberResponseDto>> ChangeStateAsync(Guid sellerId, Guid memberId, string action, CancellationToken ct = default);
    Task<Result<SellerMemberResponseDto>> SetRoleAsync(Guid sellerId, Guid memberId, string role, bool assigned, CancellationToken ct = default);
    Task<Result<SellerMemberResponseDto>> SetWarehouseAsync(Guid sellerId, Guid memberId, Guid warehouseId, bool assigned, CancellationToken ct = default);
}

public interface ISellerTeamRepository
{
    Task<Result<IReadOnlyList<SellerMemberResponseDto>>> GetMembersAsync(Guid actor, Guid sellerId, CancellationToken ct);
    Task<Result<SellerMemberResponseDto>> GetMemberAsync(Guid actor, Guid sellerId, Guid memberId, CancellationToken ct);
    Task<Result<SellerMemberResponseDto>> InviteAsync(Guid actor, Guid sellerId, InviteSellerMemberRequestDto? request, CancellationToken ct);
    Task<Result<SellerMemberResponseDto>> AcceptAsync(Guid actor, Guid sellerId, CancellationToken ct);
    Task<Result<IReadOnlyList<SellerInvitationResponseDto>>> GetInvitationsAsync(Guid actor, CancellationToken ct);
    Task<Result<SellerMemberResponseDto>> ChangeStateAsync(Guid actor, Guid sellerId, Guid memberId, string action, CancellationToken ct);
    Task<Result<SellerMemberResponseDto>> SetRoleAsync(Guid actor, Guid sellerId, Guid memberId, string role, bool assigned, CancellationToken ct);
    Task<Result<SellerMemberResponseDto>> SetWarehouseAsync(Guid actor, Guid sellerId, Guid memberId, Guid warehouseId, bool assigned, CancellationToken ct);
}

public static class SellerTeamErrors
{
    public static Error NotFound => new("seller_team.not_found", "Seller, membership, or assigned resource was not found.");
    public static Error Validation(string message) => new("seller_team.validation", message);
    public static Error Conflict(string message) => new("seller_team.conflict", message);
    public static Error Unauthorized => new("seller_team.unauthorized", "Sign in with an active account.");
}

