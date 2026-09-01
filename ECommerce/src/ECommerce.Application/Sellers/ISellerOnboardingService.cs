using ECommerce.Application.Common;
using ECommerce.Application.Sellers.Dtos;

namespace ECommerce.Application.Sellers;

public interface ISellerOnboardingService
{
    Task<Result<SellerOnboardingResponseDto>> CreateAsync(
        CreateSellerRequestDto? request,
        CancellationToken cancellationToken = default);
}