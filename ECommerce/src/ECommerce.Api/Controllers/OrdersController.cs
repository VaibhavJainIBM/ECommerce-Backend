using ECommerce.Application.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IShoppingService shoppingService) : ShoppingControllerBase
{
    private const string GetOrderRoute = "CustomerOrderById";

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderResponseDto>> CheckoutAsync(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CheckoutRequestDto? request, CancellationToken cancellationToken)
    {
        var result = await shoppingService.CheckoutAsync(idempotencyKey, request, cancellationToken);
        if (result.IsFailure) return ShoppingProblem(result.Errors);
        var checkout = result.Value!;
        Response.Headers["Idempotency-Replayed"] = checkout.Replayed ? "true" : "false";
        return checkout.Replayed
            ? Ok(checkout.Order)
            : CreatedAtRoute(GetOrderRoute, new { orderId = checkout.Order.OrderId }, checkout.Order);
    }

    [HttpGet]
    public async Task<ActionResult<PagedOrdersResponseDto>> GetAsync(
        [FromQuery] OrderQueryDto? query, CancellationToken cancellationToken)
    {
        var result = await shoppingService.GetOrdersAsync(query, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }

    [HttpGet("{orderId:guid}", Name = GetOrderRoute)]
    public async Task<ActionResult<OrderResponseDto>> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await shoppingService.GetOrderAsync(orderId, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }

    [HttpPost("{orderId:guid}/cancel")]
    public async Task<ActionResult<OrderResponseDto>> CancelAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await shoppingService.CancelOrderAsync(orderId, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }
}
