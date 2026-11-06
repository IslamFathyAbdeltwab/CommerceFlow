using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.ProductAttributes.Commands.CreateProductAttribute;

public sealed class CreateProductAttributeCommandHandler(
    IProductAttributeRepository attributes,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateProductAttributeCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductAttributeCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (await attributes.CodeExistsAsync(code, cancellationToken: cancellationToken))
            throw new ConflictException($"A product attribute with code '{code}' already exists.");

        var attribute = ProductAttribute.Create(
            request.Name,
            request.Code,
            request.DataType,
            request.IsVariantDefining,
            request.IsFilterable);

        await attributes.AddAsync(attribute, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return attribute.Id;
    }
}
