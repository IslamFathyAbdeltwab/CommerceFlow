//using CommerceFlow.Abstraction.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProductVariants;

public sealed class GetProductVariantsQueryHandler(
    IProductRepository products) : IRequestHandler<GetProductVariantsQuery, IReadOnlyList<ProductVariantDto>>
{
    public async Task<IReadOnlyList<ProductVariantDto>> Handle(GetProductVariantsQuery request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        return product.Variants.Select(v => new ProductVariantDto(
            v.Id,
            v.Name,
            v.Sku,
            v.Price,
            v.CompareAtPrice,
            v.IsActive,
            v.AttributeValues.Select(a => new ProductVariantAttributeDto(
                a.ProductAttributeId,
                a.Value)).ToList())).ToList();
    }
}