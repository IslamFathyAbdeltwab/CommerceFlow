using CommerceFlow.Application.Catalog.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Queries.GetBrandById;

public sealed class GetBrandByIdQueryHandler(
    IBrandRepository brands) : IRequestHandler<GetBrandByIdQuery, BrandDetailDto?>
{
    public async Task<BrandDetailDto?> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var brand = await brands.GetByIdAsync(request.BrandId, cancellationToken);
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
