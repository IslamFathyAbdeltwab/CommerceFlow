using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Queries.GetBrandBySlug;

public sealed record GetBrandBySlugQuery(string Slug) : IRequest<BrandDetailDto?>;
