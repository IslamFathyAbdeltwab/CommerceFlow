//using CommerceFlow.Abstraction.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(
    IProductRepository products) : IRequestHandler<GetProductByIdQuery, ProductDetailDto?>
{
    public async Task<ProductDetailDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await products.GetProductByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return null;

        var primaryImage = product.Images
            .Where(i => i.ProductVariantId is null)
            .OrderBy(i => !i.IsPrimary)
            .ThenBy(i => i.DisplayOrder)
            .FirstOrDefault();

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.BrandId,
            product.Status,
            product.IsFeatured,
            product.PublishedAtUtc,
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            primaryImage?.StorageKey,
            product.Categories.Select(c => c.CategoryId).ToList(),
            product.Variants.Select(v => new ProductVariantDto(
                v.Id,
                v.Name,
                v.Sku,
                v.Price,
                v.CompareAtPrice,
                v.IsActive,
                v.AttributeValues.Select(a => new ProductVariantAttributeDto(
                    a.ProductAttributeId,
                    a.Value)).ToList())).ToList(),
            product.Images.Select(i => new ProductImageDto(
                i.Id,
                i.ProductVariantId,
                i.StorageKey,
                i.AltText,
                i.DisplayOrder,
                i.IsPrimary)).ToList());
    }
}