using MediatR;

namespace CommerceFlow.Application.Returns.Commands.RejectReturn;

// Merchant declines the RMA; the reason is kept for support/audit and shown
// to the customer.
public sealed record RejectReturnCommand(Guid ReturnId, string Reason) : IRequest;
