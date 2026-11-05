using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDetailDto?>;