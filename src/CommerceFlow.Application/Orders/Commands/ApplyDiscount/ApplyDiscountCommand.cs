using MediatR;

namespace CommerceFlow.Application.Orders.Commands.ApplyDiscount;

public sealed record ApplyDiscountCommand(
    Guid OrderId,
    decimal DiscountAmount) : IRequest;
