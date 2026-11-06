using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Queries.GetBrandById;

public sealed record GetBrandByIdQuery(Guid BrandId) : IRequest<BrandDetailDto?>;
