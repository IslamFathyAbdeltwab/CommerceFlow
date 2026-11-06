using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.DeactivateVariant;

public sealed class DeactivateVariantCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<DeactivateVariantCommand>
{
    public async Task Handle(DeactivateVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.DeactivateVariant(request.VariantId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
