//using CommerceFlow.Abstraction.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetFeaturedProducts;

public sealed class GetFeaturedProductsQueryHandler(
    IProductRepository products) : IRequestHandler<GetFeaturedProductsQuery, IReadOnlyList<ProductListItemDto>>
{
    public async Task<IReadOnlyList<ProductListItemDto>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
    {
        var list = await products.GetFeaturedProductsAsync(request.Take, cancellationToken);

        return list.Select(p =>
        {
            var primaryImage = p.Images
                .Where(i => i.ProductVariantId is null)
                .OrderBy(i => !i.IsPrimary)
                .ThenBy(i => i.DisplayOrder)
                .FirstOrDefault();

            var minPrice = p.Variants.Where(v => v.IsActive).Select(v => v.Price).DefaultIfEmpty().Min();
            var bestVariant = p.Variants.Where(v => v.IsActive).OrderBy(v => v.Price).FirstOrDefault();

            return new ProductListItemDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Description,
                p.BrandId,
                p.IsFeatured,
                p.PublishedAtUtc,
                minPrice,
                bestVariant?.Price,
                primaryImage?.StorageKey,
                p.Categories.Select(c => c.CategoryId).ToList());
        }).ToList();
    }
}