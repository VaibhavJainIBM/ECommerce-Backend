using ECommerce.Application.Common;
using ECommerce.Application.Sellers.Dtos;

namespace ECommerce.Application.Sellers;

public interface ISellerQueryService
{
    Task<Result<IReadOnlyCollection<MySellerResponseDto>>>
        GetMineAsync(
            CancellationToken cancellationToken = default);
}