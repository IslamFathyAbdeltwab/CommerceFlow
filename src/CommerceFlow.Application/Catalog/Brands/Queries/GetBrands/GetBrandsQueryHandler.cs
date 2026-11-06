using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Catalog.Products.Queries;
using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Queries.GetBrands;

public sealed class GetBrandsQueryHandler(
    IBrandRepository brands) : IRequestHandler<GetBrandsQuery, PagedResult<BrandListItemDto>>
{
    public async Task<PagedResult<BrandListItemDto>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await brands.CountAsync(
            request.SearchTerm?.Trim(),
            request.IsActive,
            cancellationToken);

        var list = await brands.GetAllAsync(
            request.SearchTerm?.Trim(),
            request.IsActive,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(b => new BrandListItemDto(
            b.Id,
            b.Name,
            b.Slug,
            b.Description,
            b.LogoUrl,
            b.IsActive,
            b.CreatedAtUtc,
            b.UpdatedAtUtc)).ToList();

        return new PagedResult<BrandListItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
