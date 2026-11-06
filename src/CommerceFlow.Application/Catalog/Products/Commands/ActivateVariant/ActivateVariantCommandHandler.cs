using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.ActivateVariant;

public sealed class ActivateVariantCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<ActivateVariantCommand>
{
    public async Task Handle(ActivateVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.ActivateVariant(request.VariantId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
