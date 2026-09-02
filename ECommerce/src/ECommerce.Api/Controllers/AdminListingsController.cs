using ECommerce.Application.Authorization;
using ECommerce.Application.Common;
using ECommerce.Application.Listings;
using ECommerce.Application.Listings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize(Roles = PlatformRoleNames.PlatformAdmin)]
[ApiController]
[Route("api/admin/sellers/{sellerId:guid}/listings")]
public sealed class AdminListingsController(
    ISellerListingService listingService)
    : ControllerBase
{
    [HttpPost("{listingId:guid}/approve")]
    public async Task<ActionResult<SellerListingResponseDto>>
        ApproveAsync(
            Guid sellerId,
            Guid listingId,
            [FromBody]
            ChangeSellerListingStatusRequestDto? request,
            CancellationToken cancellationToken)
    {
        var result = await listingService.ApproveAsync(
            sellerId,
            listingId,
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    private ActionResult ToProblem(
        IReadOnlyCollection<Error> errors)
    {
        var error = errors.First();

        var statusCode = error.Code switch
        {
            SellerListingErrors.SellerNotFoundCode or
            SellerListingErrors.ListingNotFoundCode
                => StatusCodes.Status404NotFound,

            SellerListingErrors.SellerUnavailableCode or
            SellerListingErrors.ListingStateConflictCode or
            SellerListingErrors.ConcurrencyConflictCode
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            statusCode: statusCode,
            title: "Listing approval failed.",
            detail: error.Description,
            instance: HttpContext.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code
            });
    }
}