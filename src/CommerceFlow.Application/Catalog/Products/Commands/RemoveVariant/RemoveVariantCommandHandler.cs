using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.RemoveVariant;

public sealed class RemoveVariantCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveVariantCommand>
{
    public async Task Handle(RemoveVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.RemoveVariant(request.VariantId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
