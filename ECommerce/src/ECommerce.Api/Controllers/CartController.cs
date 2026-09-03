using ECommerce.Application.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/cart")]
public sealed class CartController(IShoppingService shoppingService) : ShoppingControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartResponseDto>> GetAsync(CancellationToken cancellationToken)
    {
        var result = await shoppingService.GetCartAsync(cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }

    [HttpPut("items/{listingId:guid}")]
    public async Task<ActionResult<CartResponseDto>> SetItemAsync(
        Guid listingId, [FromBody] SetCartItemRequestDto? request, CancellationToken cancellationToken)
    {
        var result = await shoppingService.SetCartItemAsync(listingId, request, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }

    [HttpDelete("items/{listingId:guid}")]
    public async Task<ActionResult<CartResponseDto>> RemoveItemAsync(Guid listingId, CancellationToken cancellationToken)
    {
        var result = await shoppingService.RemoveCartItemAsync(listingId, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }

    [HttpDelete]
    public async Task<ActionResult<CartResponseDto>> ClearAsync(CancellationToken cancellationToken)
    {
        var result = await shoppingService.ClearCartAsync(cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }
}
