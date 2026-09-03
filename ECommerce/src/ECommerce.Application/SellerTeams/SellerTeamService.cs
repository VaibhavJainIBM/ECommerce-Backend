using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Common;
namespace ECommerce.Application.SellerTeams;

public sealed class SellerTeamService(ISellerTeamRepository repository, ICurrentUser currentUser) : ISellerTeamService
{
    private Task<Result<T>> AsUser<T>(Func<Guid, Task<Result<T>>> action) =>
        currentUser.UserId is Guid id && id != Guid.Empty ? action(id) : Task.FromResult(Result<T>.Failure(SellerTeamErrors.Unauthorized));

    public Task<Result<IReadOnlyList<SellerMemberResponseDto>>> GetMembersAsync(Guid sellerId, CancellationToken ct = default) =>
        AsUser(id => repository.GetMembersAsync(id, sellerId, ct));
    public Task<Result<SellerMemberResponseDto>> GetMemberAsync(Guid sellerId, Guid memberId, CancellationToken ct = default) =>
        AsUser(id => repository.GetMemberAsync(id, sellerId, memberId, ct));
    public Task<Result<SellerMemberResponseDto>> InviteAsync(Guid sellerId, InviteSellerMemberRequestDto? request, CancellationToken ct = default) =>
        AsUser(id => repository.InviteAsync(id, sellerId, request, ct));
    public Task<Result<SellerMemberResponseDto>> AcceptAsync(Guid sellerId, CancellationToken ct = default) =>
        AsUser(id => repository.AcceptAsync(id, sellerId, ct));
    public Task<Result<IReadOnlyList<SellerInvitationResponseDto>>> GetInvitationsAsync(CancellationToken ct = default) =>
        AsUser(id => repository.GetInvitationsAsync(id, ct));
    public Task<Result<SellerMemberResponseDto>> ChangeStateAsync(Guid sellerId, Guid memberId, string action, CancellationToken ct = default) =>
        AsUser(id => repository.ChangeStateAsync(id, sellerId, memberId, action, ct));
    public Task<Result<SellerMemberResponseDto>> SetRoleAsync(Guid sellerId, Guid memberId, string role, bool assigned, CancellationToken ct = default) =>
        AsUser(id => repository.SetRoleAsync(id, sellerId, memberId, role, assigned, ct));
    public Task<Result<SellerMemberResponseDto>> SetWarehouseAsync(Guid sellerId, Guid memberId, Guid warehouseId, bool assigned, CancellationToken ct = default) =>
        AsUser(id => repository.SetWarehouseAsync(id, sellerId, memberId, warehouseId, assigned, ct));
}

