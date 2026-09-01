using ECommerce.Application.Sellers.Dtos;

namespace ECommerce.Application.Abstractions.Persistence;

public interface ISellerQueryRepository
{
    Task<IReadOnlyCollection<MySellerResponseDto>>
        GetForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
}