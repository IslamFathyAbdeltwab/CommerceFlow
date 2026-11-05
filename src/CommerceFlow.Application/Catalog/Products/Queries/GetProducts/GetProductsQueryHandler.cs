//using CommerceFlow.Abstraction.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(
    IProductRepository products) : IRequestHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    public async Task<PagedResult<ProductListItemDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await products.CountPublishedProductsAsync(
            request.SearchTerm?.Trim(),
            request.BrandId,
            request.CategoryId,
            request.IsFeatured,
            request.MinPrice,
            request.MaxPrice,
            cancellationToken);

        var list = await products.GetPublishedProductsAsync(
            request.SearchTerm?.Trim(),
            request.BrandId,
            request.CategoryId,
            request.IsFeatured,
            request.MinPrice,
            request.MaxPrice,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(p =>
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

        return new PagedResult<ProductListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}