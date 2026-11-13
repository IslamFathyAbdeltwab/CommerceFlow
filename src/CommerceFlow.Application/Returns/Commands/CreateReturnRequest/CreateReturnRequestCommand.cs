using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Returns.Commands.CreateReturnRequest;

// CustomerId is deliberately absent: it is copied from the order (same call as
// CreateShipmentCommand). Unlike shipments, several return requests per order
// are allowed - the handler sums them so returned quantities can never exceed
// what was ordered. Returns the return request id.
public sealed record CreateReturnRequestCommand(
    Guid OrderId,
    ReturnReason Reason,
    IReadOnlyList<CreateReturnRequestItemDto> Items,
    string? Notes = null) : IRequest<Guid>;

public sealed record CreateReturnRequestItemDto(
    Guid ProductVariantId,
    int Quantity);
