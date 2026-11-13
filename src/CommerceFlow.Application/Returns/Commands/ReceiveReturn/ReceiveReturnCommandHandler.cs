using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Returns.Commands.ReceiveReturn;

public sealed class ReceiveReturnCommandHandler(
    IReturnRequestRepository returns,
    IUnitOfWork unitOfWork) : IRequestHandler<ReceiveReturnCommand>
{
    public async Task Handle(ReceiveReturnCommand request, CancellationToken cancellationToken)
    {
        var returnRequest = await returns.GetByIdAsync(request.ReturnId, cancellationToken)
            ?? throw new NotFoundException(nameof(ReturnRequest), request.ReturnId);

        // pre-check before the state moves (state conflicts -> 409)
        if (returnRequest.Status != ReturnStatus.Approved)
            throw new ConflictException(
                $"Only approved return requests can be received (current: {returnRequest.Status}).");

        returnRequest.MarkReceived();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
