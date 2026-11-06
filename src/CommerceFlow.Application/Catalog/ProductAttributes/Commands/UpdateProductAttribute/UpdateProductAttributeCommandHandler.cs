using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Commands.UpdateProductAttribute;

public sealed class UpdateProductAttributeCommandHandler(
    IProductAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductAttributeCommand>
{
    public async Task Handle(UpdateProductAttributeCommand request, CancellationToken cancellationToken)
    {
        var attribute = await attributes.GetByIdAsync(request.AttributeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductAttribute), request.AttributeId);

        var code = request.Code.Trim().ToLowerInvariant();
        if (await attributes.CodeExistsAsync(code, excludeAttributeId: request.AttributeId, cancellationToken: cancellationToken))
            throw new ConflictException($"A product attribute with code '{code}' already exists.");

        attribute.UpdateDetails(
            request.Name,
            request.Code,
            request.DataType,
            request.IsVariantDefining,
            request.IsFilterable);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
