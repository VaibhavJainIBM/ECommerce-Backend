using ECommerce.Api.Authorization;
using ECommerce.Application.Fulfillment;
using ECommerce.Application.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Policy = SellerPolicies.Management)]
[Route("api/sellers/{sellerId:guid}/orders")]
public sealed class SellerFulfillmentController(IFulfillmentService fulfillmentService)
    : ShoppingControllerBase
{
    // This records seller dispatch only; it is not courier tracking or delivery confirmation.
    [HttpPost("{orderId:guid}/ship")]
    public async Task<ActionResult<SellerOrderResponseDto>> ShipAsync(
        Guid sellerId, Guid orderId, CancellationToken cancellationToken)
    {
        var result = await fulfillmentService.ShipSellerOrderAsync(sellerId, orderId, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }
}
