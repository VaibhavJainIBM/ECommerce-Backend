using ECommerce.Api.Authorization;
using ECommerce.Application.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize(Policy = SellerPolicies.Management)]
[ApiController]
[Route("api/sellers/{sellerId:guid}/orders")]
public sealed class SellerOrdersController(IShoppingService shoppingService) : ShoppingControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedSellerOrdersResponseDto>> GetAsync(
        Guid sellerId, [FromQuery] OrderQueryDto? query, CancellationToken cancellationToken)
    {
        var result = await shoppingService.GetSellerOrdersAsync(sellerId, query, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }
}
