using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Sellers.Dtos;

namespace ECommerce.Application.Sellers;

public sealed class SellerQueryService(
    ICurrentUser currentUser,
    ISellerQueryRepository repository)
    : ISellerQueryService
{
    public async Task<
        Result<IReadOnlyCollection<MySellerResponseDto>>>
        GetMineAsync(
            CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            return Result<
                IReadOnlyCollection<MySellerResponseDto>>
                .Failure(
                    SellerErrors.CurrentUserUnavailable);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sellers = await repository.GetForUserAsync(
            userId.Value,
            cancellationToken);

        return Result<
            IReadOnlyCollection<MySellerResponseDto>>
            .Success(sellers);
    }
}