using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProductVariants;

public sealed record GetProductVariantsQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductVariantDto>>;