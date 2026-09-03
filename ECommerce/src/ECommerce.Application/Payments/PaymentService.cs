using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Shopping;

namespace ECommerce.Application.Payments;

public sealed class PaymentService(IPaymentRepository repository, ICurrentUser currentUser, DemoPaymentMode mode) : IPaymentService
{
    public Task<Result<CreatePaymentResponseDto>> CreateAsync(Guid orderId, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        var error = CheckAccess();
        if (error is not null) return Task.FromResult(Result<CreatePaymentResponseDto>.Failure(error));
        if (orderId == Guid.Empty || !Guid.TryParse(idempotencyKey, out var key) || key == Guid.Empty)
            return Task.FromResult(Result<CreatePaymentResponseDto>.Failure(ShoppingErrors.Validation("Order ID and a GUID Idempotency-Key header are required.")));
        return repository.CreateDemoPaymentAsync(currentUser.UserId!.Value, orderId, key, cancellationToken);
    }

    public Task<Result<PaymentResponseDto>> GetAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var error = CheckAccess();
        if (error is not null) return Task.FromResult(Result<PaymentResponseDto>.Failure(error));
        return repository.GetDemoPaymentAsync(currentUser.UserId!.Value, paymentId, cancellationToken);
    }

    public Task<Result<IReadOnlyList<PaymentResponseDto>>> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var error = CheckAccess();
        if (error is not null) return Task.FromResult(Result<IReadOnlyList<PaymentResponseDto>>.Failure(error));
        return repository.GetDemoPaymentsAsync(currentUser.UserId!.Value, orderId, cancellationToken);
    }

    public Task<Result<PaymentResponseDto>> CompleteAsync(Guid paymentId, CompleteDemoPaymentRequestDto? request, CancellationToken cancellationToken = default)
    {
        var error = CheckAccess();
        if (error is not null) return Task.FromResult(Result<PaymentResponseDto>.Failure(error));
        var outcome = request?.Outcome?.Trim();
        if (paymentId == Guid.Empty || (!string.Equals(outcome, "Succeeded", StringComparison.OrdinalIgnoreCase) &&
                                      !string.Equals(outcome, "Failed", StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(Result<PaymentResponseDto>.Failure(ShoppingErrors.Validation("A payment ID and outcome of 'Succeeded' or 'Failed' are required. This is a demo, not a real charge.")));
        return repository.CompleteDemoPaymentAsync(currentUser.UserId!.Value, paymentId,
            string.Equals(outcome, "Succeeded", StringComparison.OrdinalIgnoreCase), cancellationToken);
    }

    private Error? CheckAccess()
    {
        if (!mode.Enabled) return ShoppingErrors.NotFound("Demo payments are disabled. They require Development environment and DemoPayments:Enabled=true.");
        if (currentUser.UserId is not Guid id || id == Guid.Empty) return ShoppingErrors.Unauthenticated;
        return null;
    }
}
