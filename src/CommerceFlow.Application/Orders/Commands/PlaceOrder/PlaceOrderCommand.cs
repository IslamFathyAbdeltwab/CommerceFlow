using MediatR;

namespace CommerceFlow.Application.Orders.Commands.PlaceOrder;

public sealed record PlaceOrderCommand(
    Guid CustomerId,
    IReadOnlyList<PlaceOrderLineDto> Lines,
    PlaceOrderAddressDto ShippingAddress,
    string Currency = "USD",
    string? CustomerNote = null,
    decimal ShippingFee = 0,
    decimal DiscountAmount = 0) : IRequest<Guid>;

public sealed record PlaceOrderLineDto(
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity);

public sealed record PlaceOrderAddressDto(
    string FullName,
    string Line1,
    string City,
    string PostalCode,
    string Country,
    string? Line2 = null,
    string? State = null,
    string? PhoneNumber = null);
