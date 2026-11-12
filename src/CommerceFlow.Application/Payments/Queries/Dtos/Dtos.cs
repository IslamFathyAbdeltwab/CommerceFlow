using CommerceFlow.Domain.Entities.Payments;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Payments.Queries;

// One DTO for detail and list reads: a payment has no children, so a
// separate list-item type would add nothing (same call as AddressDto).
public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string Provider,
    string IdempotencyKey,
    string? ProviderTransactionId,
    string? FailureReason,
    DateTimeOffset? ProcessedAtUtc,
    DateTimeOffset? RefundedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

// Centralizes entity -> DTO mapping so the handlers stay thin.
internal static class PaymentMappings
{
    public static PaymentDto ToDto(Payment payment) => new(
        payment.Id,
        payment.OrderId,
        payment.CustomerId,
        payment.Amount,
        payment.Currency,
        payment.Status,
        payment.Provider,
        payment.IdempotencyKey,
        payment.ProviderTransactionId,
        payment.FailureReason,
        payment.ProcessedAtUtc,
        payment.RefundedAtUtc,
        payment.CreatedAtUtc,
        payment.UpdatedAtUtc);
}
