using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetFeaturedProducts;

public sealed record GetFeaturedProductsQuery(int Take = 8) : IRequest<IReadOnlyList<ProductListItemDto>>;