using CommerceFlow.Application.Catalog.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Queries.GetProductAttributeById;

public sealed class GetProductAttributeByIdQueryHandler(
    IProductAttributeRepository attributes) : IRequestHandler<GetProductAttributeByIdQuery, ProductAttributeDto?>
{
    public async Task<ProductAttributeDto?> Handle(GetProductAttributeByIdQuery request, CancellationToken cancellationToken)
    {
        var attribute = await attributes.GetByIdAsync(request.AttributeId, cancellationToken);
        if (attribute is null)
            return null;

        return new ProductAttributeDto(
            attribute.Id,
            attribute.Name,
            attribute.Code,
            attribute.DataType,
            attribute.IsVariantDefining,
            attribute.IsFilterable,
            attribute.CreatedAtUtc,
            attribute.UpdatedAtUtc);
    }
}
