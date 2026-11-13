using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Returns.Commands.RejectReturn;

public sealed class RejectReturnCommandHandler(
    IReturnRequestRepository returns,
    IUnitOfWork unitOfWork) : IRequestHandler<RejectReturnCommand>
{
    public async Task Handle(RejectReturnCommand request, CancellationToken cancellationToken)
    {
        var returnRequest = await returns.GetByIdAsync(request.ReturnId, cancellationToken)
            ?? throw new NotFoundException(nameof(ReturnRequest), request.ReturnId);

        // pre-check before the state moves (state conflicts -> 409): a
        // decision is only possible while the request is still waiting
        if (returnRequest.Status != ReturnStatus.Pending)
            throw new ConflictException(
                $"Only pending return requests can be rejected (current: {returnRequest.Status}).");

        returnRequest.Reject(request.Reason.Trim());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
