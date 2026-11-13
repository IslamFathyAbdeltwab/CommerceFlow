using MediatR;

namespace CommerceFlow.Application.Returns.Commands.ApproveReturn;

// Merchant accepts the RMA - the customer may now send the goods back.
public sealed record ApproveReturnCommand(Guid ReturnId) : IRequest;
