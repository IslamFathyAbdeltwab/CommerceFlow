using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProductBySlug;

public sealed record GetProductBySlugQuery(string Slug) : IRequest<ProductDetailDto?>;