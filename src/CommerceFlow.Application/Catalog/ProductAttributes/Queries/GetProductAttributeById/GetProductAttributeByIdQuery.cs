using MediatR;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributeById;

public sealed record GetProductAttributeByIdQuery(Guid AttributeId) : IRequest<ProductAttributeDto?>;
