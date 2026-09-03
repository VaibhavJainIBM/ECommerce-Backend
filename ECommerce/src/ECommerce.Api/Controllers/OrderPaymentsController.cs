using ECommerce.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/orders/{orderId:guid}/payments")]
public sealed class OrderPaymentsController(IPaymentService paymentService) : ShoppingControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PaymentResponseDto>> CreateAsync(
        Guid orderId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.CreateAsync(orderId, idempotencyKey, cancellationToken);
        if (result.IsFailure) return ShoppingProblem(result.Errors);
        var creation = result.Value!;
        Response.Headers["Idempotency-Replayed"] = creation.Replayed ? "true" : "false";
        return creation.Replayed ? Ok(creation.Payment) :
            CreatedAtRoute(DemoPaymentsController.GetPaymentRoute,
                new { paymentId = creation.Payment.PaymentId }, creation.Payment);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentResponseDto>>> GetAsync(
        Guid orderId, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetForOrderAsync(orderId, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }
}
