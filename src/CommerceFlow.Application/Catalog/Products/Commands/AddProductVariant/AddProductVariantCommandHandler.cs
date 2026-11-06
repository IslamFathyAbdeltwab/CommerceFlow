using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.AddProductVariant;

public sealed class AddProductVariantCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<AddProductVariantCommand, Guid>
{
    public async Task<Guid> Handle(AddProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await products.SkuExistsAsync(sku, cancellationToken: cancellationToken))
            throw new ConflictException($"A product variant with SKU '{sku}' already exists.");

        var variantId = product.AddVariant(request.Name, request.Sku, request.Price, request.CompareAtPrice);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return variantId;
    }
}
