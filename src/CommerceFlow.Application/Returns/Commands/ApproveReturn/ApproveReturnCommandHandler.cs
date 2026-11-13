using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Returns.Commands.ApproveReturn;

public sealed class ApproveReturnCommandHandler(
    IReturnRequestRepository returns,
    IUnitOfWork unitOfWork) : IRequestHandler<ApproveReturnCommand>
{
    public async Task Handle(ApproveReturnCommand request, CancellationToken cancellationToken)
    {
        var returnRequest = await returns.GetByIdAsync(request.ReturnId, cancellationToken)
            ?? throw new NotFoundException(nameof(ReturnRequest), request.ReturnId);

        // pre-check before the state moves (state conflicts -> 409)
        if (returnRequest.Status != ReturnStatus.Pending)
            throw new ConflictException(
                $"Only pending return requests can be approved (current: {returnRequest.Status}).");

        returnRequest.Approve();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
