using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Sellers.Dtos;
using ECommerce.Domain.Constants;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Sellers;

public sealed class SellerOnboardingService(
    ICurrentUser currentUser,
    ISellerOnboardingRepository repository)
    : ISellerOnboardingService
{
    public async Task<Result<SellerOnboardingResponseDto>>
        CreateAsync(
            CreateSellerRequestDto? request,
            CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            return Result<SellerOnboardingResponseDto>.Failure(
                SellerErrors.CurrentUserUnavailable);
        }

        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return Result<SellerOnboardingResponseDto>.Failure(
                validationErrors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var displayName =
            request!.DisplayName!.Trim();

        var legalBusinessName =
            request.LegalBusinessName!.Trim();

        var seller = new Seller(
            displayName,
            legalBusinessName);

        var ownerMember = new SellerMember(
            seller.Id,
            userId.Value);

        // The creator is already authenticated.
        // They do not need to accept an invitation.
        ownerMember.Activate();

        var ownerRole = new SellerRole(
            seller.Id,
            SellerRoleNames.Owner,
            "Full control over this seller account.",
            isBuiltIn: true);

        var ownerRoleAssignment =
            new SellerMemberRole(
                ownerMember,
                ownerRole);

        await repository.CreateSellerWithOwnerAsync(
            seller,
            ownerMember,
            ownerRole,
            ownerRoleAssignment,
            cancellationToken);

        var response = new SellerOnboardingResponseDto(
            seller.Id,
            seller.DisplayName,
            seller.LegalBusinessName,
            seller.Status.ToString(),
            ownerMember.Id,
            ownerMember.Status.ToString(),
            ownerRole.Id,
            ownerRole.Name,
            seller.CreatedAtUtc);

        return Result<SellerOnboardingResponseDto>.Success(
            response);
    }

    private static List<Error> Validate(
        CreateSellerRequestDto? request)
    {
        var errors = new List<Error>();

        if (request is null)
        {
            errors.Add(SellerErrors.RequestRequired);
            return errors;
        }

        if (string.IsNullOrWhiteSpace(
                request.DisplayName))
        {
            errors.Add(
                SellerErrors.DisplayNameRequired);
        }
        else if (
            request.DisplayName.Trim().Length > 150)
        {
            errors.Add(
                SellerErrors.DisplayNameTooLong);
        }

        if (string.IsNullOrWhiteSpace(
                request.LegalBusinessName))
        {
            errors.Add(
                SellerErrors.LegalBusinessNameRequired);
        }
        else if (
            request.LegalBusinessName.Trim().Length > 250)
        {
            errors.Add(
                SellerErrors.LegalBusinessNameTooLong);
        }

        return errors;
    }
}