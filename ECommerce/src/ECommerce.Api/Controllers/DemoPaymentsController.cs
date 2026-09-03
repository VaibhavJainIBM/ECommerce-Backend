using ECommerce.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

// All actions are additionally gated in PaymentService by a server-owned development flag.
[Authorize]
[ApiController]
[Route("api/payments")]
public sealed class DemoPaymentsController(IPaymentService paymentService) : ShoppingControllerBase
{
    public const string GetPaymentRoute = "DemoPaymentById";

    [HttpGet("{paymentId:guid}", Name = GetPaymentRoute)]
    public async Task<ActionResult<PaymentResponseDto>> GetAsync(
        Guid paymentId, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetAsync(paymentId, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }

    [HttpPost("{paymentId:guid}/demo-complete")]
    public async Task<ActionResult<PaymentResponseDto>> CompleteAsync(
        Guid paymentId, [FromBody] CompleteDemoPaymentRequestDto? request,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.CompleteAsync(paymentId, request, cancellationToken);
        return result.IsFailure ? ShoppingProblem(result.Errors) : Ok(result.Value);
    }
}
