using CommerceFlow.Application.Catalog.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Queries.GetBrandBySlug;

public sealed class GetBrandBySlugQueryHandler(
    IBrandRepository brands) : IRequestHandler<GetBrandBySlugQuery, BrandDetailDto?>
{
    public async Task<BrandDetailDto?> Handle(GetBrandBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var brand = await brands.GetBySlugAsync(slug, cancellationToken);
        if (brand is null)
            return null;

        return new BrandDetailDto(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.Description,
            brand.LogoUrl,
            brand.IsActive,
            brand.CreatedAtUtc,
            brand.UpdatedAtUtc);
    }
}
