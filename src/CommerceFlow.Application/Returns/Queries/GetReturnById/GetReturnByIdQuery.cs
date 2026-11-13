using MediatR;

namespace CommerceFlow.Application.Returns.Queries.GetReturnById;

// null = not found (the controller maps it to 404, same as GetShipmentById).
public sealed record GetReturnByIdQuery(Guid ReturnId) : IRequest<ReturnRequestDto?>;
