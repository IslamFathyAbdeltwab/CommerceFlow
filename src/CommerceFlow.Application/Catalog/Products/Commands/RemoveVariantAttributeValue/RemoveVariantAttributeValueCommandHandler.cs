using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.RemoveVariantAttributeValue;

public sealed class RemoveVariantAttributeValueCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveVariantAttributeValueCommand>
{
    public async Task Handle(RemoveVariantAttributeValueCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.RemoveVariantAttributeValue(request.VariantId, request.AttributeId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
