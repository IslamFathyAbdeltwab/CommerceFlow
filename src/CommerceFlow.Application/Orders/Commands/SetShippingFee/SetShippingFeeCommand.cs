using MediatR;

namespace CommerceFlow.Application.Orders.Commands.SetShippingFee;

public sealed record SetShippingFeeCommand(
    Guid OrderId,
    decimal ShippingFee) : IRequest;
