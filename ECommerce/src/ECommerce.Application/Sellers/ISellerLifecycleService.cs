using ECommerce.Application.Common;
using ECommerce.Application.Sellers.Dtos;

namespace ECommerce.Application.Sellers;

public interface ISellerLifecycleService
{
    Task<Result<SellerLifecycleResponseDto>>
        SubmitForReviewAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default);

    Task<Result<SellerLifecycleResponseDto>> ApproveAsync(
        Guid sellerId,
        CancellationToken cancellationToken = default);
}