using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProductStats;

public sealed record GetProductStatsQuery : IRequest<ProductStatsDto>;