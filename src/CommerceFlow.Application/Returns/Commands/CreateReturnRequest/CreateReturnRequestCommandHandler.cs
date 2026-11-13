using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Returns.Interfaces;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Returns;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Returns.Commands.CreateReturnRequest;

public sealed class CreateReturnRequestCommandHandler(
    IReturnRequestRepository returns,
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateReturnRequestCommand, Guid>
{
    public async Task<Guid> Handle(CreateReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // pre-checks before anything is created (state conflicts -> 409):
        // goods can only go back once the customer actually has them
        if (order.Status != OrderStatus.Delivered)
            throw new ConflictException(
                $"Only delivered orders can be returned (current: {order.Status}).");

        var existing = await returns.GetByOrderIdAsync(order.Id, cancellationToken);

        var returnRequest = ReturnRequest.Create(
            order.Id,
            order.CustomerId,
            request.Reason,
            request.Notes);

        foreach (var item in request.Items)
        {
            var line = order.Lines.FirstOrDefault(l => l.ProductVariantId == item.ProductVariantId)
                ?? throw new ConflictException(
                    "Return item does not match any order line.");

            // Rejected/Cancelled requests released their claim on the qty; a
            // Completed one already took the goods back, so it stays claimed
            var alreadyReturned = existing
                .Where(r => r.Status is not (ReturnStatus.Rejected or ReturnStatus.Cancelled))
                .SelectMany(r => r.Items)
                .Where(i => i.ProductVariantId == item.ProductVariantId)
                .Sum(i => i.Quantity);

            if (item.Quantity > line.Quantity - alreadyReturned)
                throw new ConflictException(
                    "Return quantity exceeds the returnable quantity.");

            returnRequest.AddItem(item.ProductVariantId, item.Quantity);
        }

        await returns.AddAsync(returnRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return returnRequest.Id;
    }
}
