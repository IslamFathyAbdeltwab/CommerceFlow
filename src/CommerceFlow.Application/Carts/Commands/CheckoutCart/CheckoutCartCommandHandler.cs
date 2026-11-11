using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Carts.Commands.CheckoutCart;

public sealed class CheckoutCartCommandHandler(
    ICartRepository carts,
    IUnitOfWork unitOfWork) : IRequestHandler<CheckoutCartCommand>
{
    public async Task Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await carts.GetByIdAsync(request.CartId, cancellationToken)
            ?? throw new NotFoundException(nameof(Cart), request.CartId);

        if (cart.Status != CartStatus.Active)
            throw new ConflictException($"Only active carts can be checked out (current: {cart.Status}).");

        if (cart.Lines.Count == 0)
            throw new ConflictException("Cannot check out an empty cart.");

        cart.MarkCheckedOut();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
