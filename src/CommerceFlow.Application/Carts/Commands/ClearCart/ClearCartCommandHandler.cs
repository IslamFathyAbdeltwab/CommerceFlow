using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Carts.Commands.ClearCart;

public sealed class ClearCartCommandHandler(
    ICartRepository carts,
    IUnitOfWork unitOfWork) : IRequestHandler<ClearCartCommand>
{
    public async Task Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await carts.GetByIdAsync(request.CartId, cancellationToken)
            ?? throw new NotFoundException(nameof(Cart), request.CartId);

        if (cart.Status != CartStatus.Active)
            throw new ConflictException("Cart items can only be changed while the cart is active.");

        cart.Clear();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
