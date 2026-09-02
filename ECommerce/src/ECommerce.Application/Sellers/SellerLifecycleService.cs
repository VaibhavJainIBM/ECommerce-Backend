using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Sellers.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Sellers;

public sealed class SellerLifecycleService(
    ISellerLifecycleRepository repository)
    : ISellerLifecycleService
{
    public async Task<Result<SellerLifecycleResponseDto>>
        SubmitForReviewAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default)
    {
        if (sellerId == Guid.Empty)
        {
            return Result<SellerLifecycleResponseDto>.Failure(
                SellerLifecycleErrors.SellerIdRequired);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var seller = await repository.GetTrackedAsync(
            sellerId,
            cancellationToken);

        if (seller is null)
        {
            return Result<SellerLifecycleResponseDto>.Failure(
                SellerLifecycleErrors.SellerNotFound(
                    sellerId));
        }

        if (seller.Status !=
                SellerStatus.PendingVerification &&
            seller.Status != SellerStatus.Rejected)
        {
            return Result<SellerLifecycleResponseDto>.Failure(
                SellerLifecycleErrors.CannotSubmitForReview(
                    seller.Status.ToString()));
        }

        seller.SubmitForReview();

        await repository.SaveChangesAsync(
            cancellationToken);

        return Result<SellerLifecycleResponseDto>.Success(
            Map(seller));
    }

    public async Task<Result<SellerLifecycleResponseDto>>
        ApproveAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default)
    {
        if (sellerId == Guid.Empty)
        {
            return Result<SellerLifecycleResponseDto>.Failure(
                SellerLifecycleErrors.SellerIdRequired);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var seller = await repository.GetTrackedAsync(
            sellerId,
            cancellationToken);

        if (seller is null)
        {
            return Result<SellerLifecycleResponseDto>.Failure(
                SellerLifecycleErrors.SellerNotFound(
                    sellerId));
        }

        if (seller.Status != SellerStatus.UnderReview)
        {
            return Result<SellerLifecycleResponseDto>.Failure(
                SellerLifecycleErrors.CannotApprove(
                    seller.Status.ToString()));
        }

        seller.Approve();

        await repository.SaveChangesAsync(
            cancellationToken);

        return Result<SellerLifecycleResponseDto>.Success(
            Map(seller));
    }

    private static SellerLifecycleResponseDto Map(
        Seller seller)
    {
        return new SellerLifecycleResponseDto(
            seller.Id,
            seller.DisplayName,
            seller.LegalBusinessName,
            seller.Status.ToString(),
            seller.ApprovedAtUtc,
            seller.CreatedAtUtc,
            seller.UpdatedAtUtc);
    }
}