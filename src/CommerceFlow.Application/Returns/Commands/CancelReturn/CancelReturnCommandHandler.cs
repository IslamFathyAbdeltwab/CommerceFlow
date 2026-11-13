using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Returns.Commands.CancelReturn;

public sealed class CancelReturnCommandHandler(
    IReturnRequestRepository returns,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelReturnCommand>
{
    public async Task Handle(CancelReturnCommand request, CancellationToken cancellationToken)
    {
        var returnRequest = await returns.GetByIdAsync(request.ReturnId, cancellationToken)
            ?? throw new NotFoundException(nameof(ReturnRequest), request.ReturnId);

        // pre-check before the state moves (state conflicts -> 409): once the
        // goods are on the way back or already received, cancelling loses them
        if (returnRequest.Status is not (ReturnStatus.Pending or ReturnStatus.Approved))
            throw new ConflictException(
                $"Only pending or approved return requests can be cancelled (current: {returnRequest.Status}).");

        returnRequest.Cancel();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
