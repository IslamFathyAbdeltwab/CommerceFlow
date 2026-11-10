using MediatR;

namespace CommerceFlow.Application.Orders.Commands.AddOrderLine;

public sealed record AddOrderLineCommand(
    Guid OrderId,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity) : IRequest;
