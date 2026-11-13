using MediatR;

namespace CommerceFlow.Application.Returns.Commands.CancelReturn;

// Withdrawn by the customer while still pending, or revoked by the merchant
// after approving but before receiving.
public sealed record CancelReturnCommand(Guid ReturnId) : IRequest;
