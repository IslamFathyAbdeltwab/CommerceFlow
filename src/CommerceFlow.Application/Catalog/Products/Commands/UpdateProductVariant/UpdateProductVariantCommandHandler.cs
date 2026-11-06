using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductVariantCommand>
{
    public async Task Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await products.SkuExistsAsync(sku, excludeVariantId: request.VariantId, cancellationToken: cancellationToken))
            throw new ConflictException($"A product variant with SKU '{sku}' already exists.");

        product.UpdateVariantDetails(
            request.VariantId,
            request.Name,
            request.Sku,
            request.Barcode,
            request.Price,
            request.CompareAtPrice,
            request.WeightGrams);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
