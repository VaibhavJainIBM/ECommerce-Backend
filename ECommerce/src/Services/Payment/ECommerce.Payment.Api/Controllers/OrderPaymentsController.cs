using ECommerce.Payment.Application.Contracts;
using ECommerce.Payment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Payment.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/orders/{orderId:guid}/payments")]
public sealed class OrderPaymentsController(
    IPaymentService paymentService)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> CreateAsync(
        Guid orderId,
        [FromHeader(Name = "Idempotency-Key")]
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result =
            await paymentService.CreateAsync(
                orderId,
                idempotencyKey,
                cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.First();

            return Problem(
                statusCode: GetStatusCode(error.Code),
                title: "Payment request failed.",
                detail: error.Description);
        }

        var creation = result.Value!;

        Response.Headers["Idempotency-Replayed"] =
            creation.Replayed
                ? "true"
                : "false";

        return creation.Replayed
            ? Ok(creation.Payment)
            : Created(
                $"/api/payments/{creation.Payment.PaymentId}",
                creation.Payment);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentResponse>>> GetAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var result =
            await paymentService.GetForOrderAsync(
                orderId,
                cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Errors.First();

            return Problem(
                statusCode: GetStatusCode(error.Code),
                title: "Payment request failed.",
                detail: error.Description);
        }

        return Ok(result.Value);
    }

    private static int GetStatusCode(string code)
    {
        return code switch
        {
            "Payment.Unauthenticated" => 401,
            "Payment.NotFound" => 404,
            "Payment.Conflict" => 409,
            "Payment.Dependency" => 503,
            _ => 400
        };
    }
}