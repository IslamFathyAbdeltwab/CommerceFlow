using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Returns.Commands.CompleteReturn;

public sealed class CompleteReturnCommandHandler(
    IReturnRequestRepository returns,
    IUnitOfWork unitOfWork) : IRequestHandler<CompleteReturnCommand>
{
    public async Task Handle(CompleteReturnCommand request, CancellationToken cancellationToken)
    {
        var returnRequest = await returns.GetByIdAsync(request.ReturnId, cancellationToken)
            ?? throw new NotFoundException(nameof(ReturnRequest), request.ReturnId);

        // pre-check before the state moves (state conflicts -> 409): nothing
        // completes until the warehouse has scanned the goods in
        if (returnRequest.Status != ReturnStatus.Received)
            throw new ConflictException(
                $"Only received return requests can be completed (current: {returnRequest.Status}).");

        returnRequest.Complete();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
