using ECommerce.Payment.Application.Contracts;
using ECommerce.Payment.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Payment.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(
    IPaymentService paymentService)
    : ControllerBase
{
    [HttpGet("{paymentId:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetAsync(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var result =
            await paymentService.GetAsync(
                paymentId,
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

    [HttpPost("{paymentId:guid}/demo-complete")]
    public async Task<ActionResult<PaymentResponse>> CompleteAsync(
        Guid paymentId,
        [FromBody] CompletePaymentRequest? request,
        CancellationToken cancellationToken)
    {
        var result =
            await paymentService.CompleteAsync(
                paymentId,
                request,
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