using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Carts.Interfaces;
using CommerceFlow.Domain.Entities.Carts;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Carts.Commands.AbandonCart;

public sealed class AbandonCartCommandHandler(
    ICartRepository carts,
    IUnitOfWork unitOfWork) : IRequestHandler<AbandonCartCommand>
{
    public async Task Handle(AbandonCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await carts.GetByIdAsync(request.CartId, cancellationToken)
            ?? throw new NotFoundException(nameof(Cart), request.CartId);

        if (cart.Status != CartStatus.Active)
            throw new ConflictException($"Only active carts can be abandoned (current: {cart.Status}).");

        cart.MarkAbandoned();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
