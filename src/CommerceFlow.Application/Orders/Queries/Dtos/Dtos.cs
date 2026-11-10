using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Orders.Queries;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    OrderStatus Status,
    OrderPaymentStatus PaymentStatus,
    string Currency,
    AddressDto ShippingAddress,
    decimal Subtotal,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal GrandTotal,
    string? CustomerNote,
    string? CancelReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? ShippedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? RefundedAtUtc,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record AddressDto(
    string FullName,
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    string? PhoneNumber);

public sealed record OrderLineDto(
    Guid Id,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderListItemDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    OrderStatus Status,
    OrderPaymentStatus PaymentStatus,
    string Currency,
    decimal GrandTotal,
    DateTimeOffset CreatedAtUtc);
