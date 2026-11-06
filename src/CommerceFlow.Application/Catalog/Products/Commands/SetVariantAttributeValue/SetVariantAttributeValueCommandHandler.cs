using System.Globalization;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.SetVariantAttributeValue;

public sealed class SetVariantAttributeValueCommandHandler(
    IProductRepository products,
    IProductAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IRequestHandler<SetVariantAttributeValueCommand>
{
    public async Task Handle(SetVariantAttributeValueCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (product.Variants.All(v => v.Id != request.VariantId))
            throw new NotFoundException(nameof(ProductVariant), request.VariantId);

        var attribute = await attributes.GetByIdAsync(request.AttributeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductAttribute), request.AttributeId);

        if (!IsValueValidForDataType(attribute.DataType, request.Value))
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(SetVariantAttributeValueCommand.Value),
                    $"Value must be a valid {attribute.DataType.ToString().ToLowerInvariant()}.")
            });

        product.SetVariantAttributeValue(request.VariantId, request.AttributeId, request.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static bool IsValueValidForDataType(ProductAttributeDataType dataType, string value)
    {
        var trimmed = value.Trim();

        return dataType switch
        {
            ProductAttributeDataType.Number =>
                decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            ProductAttributeDataType.Boolean => bool.TryParse(trimmed, out _),
            _ => true
        };
    }
}
