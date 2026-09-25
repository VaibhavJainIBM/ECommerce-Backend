using ECommerce.Order.Application.Common;
using ECommerce.Order.Application.Contracts;
using ECommerce.Order.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Order.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(
    IOrderService orderService)
    : ControllerBase
{
    [HttpPost]
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutOrderResponse>> CheckoutAsync(
        [FromBody] CheckoutOrderRequest? request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await orderService.CheckoutAsync(
            request,
            idempotencyKey,
            cancellationToken);

        if (result.IsFailure)
            return ToProblem(result.Errors.First());

        var response = result.Value!;

        if (response.Replayed)
            return Ok(response);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { orderId = response.Order.OrderId },
            response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetMineAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetMineAsync(
            page,
            pageSize,
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.Errors.First())
            : Ok(result.Value);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderResponse>> GetByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result = await orderService.GetAsync(
            orderId,
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.Errors.First())
            : Ok(result.Value);
    }

    [HttpPost("{orderId:guid}/cancel")]
    public async Task<ActionResult<OrderResponse>> CancelAsync(
        Guid orderId,
        [FromBody] CancelOrderRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await orderService.CancelAsync(
            orderId,
            request,
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.Errors.First())
            : Ok(result.Value);
    }

    [HttpPost("{orderId:guid}/payment-confirmation")]
    public async Task<ActionResult<OrderResponse>> ConfirmPaymentAsync(
        Guid orderId,
        [FromBody] ConfirmOrderPaymentRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await orderService.ConfirmPaymentAsync(
            orderId,
            request,
            cancellationToken);

        return result.IsFailure
            ? ToProblem(result.Errors.First())
            : Ok(result.Value);
    }

    private ObjectResult ToProblem(Error error)
    {
        var statusCode = error.Code switch
        {
            "Order.Unauthenticated" => StatusCodes.Status401Unauthorized,
            "Order.NotFound" => StatusCodes.Status404NotFound,
            "Order.Conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            statusCode: statusCode,
            title: "Order request failed.",
            detail: error.Description,
            instance: HttpContext.Request.Path);
    }
}
