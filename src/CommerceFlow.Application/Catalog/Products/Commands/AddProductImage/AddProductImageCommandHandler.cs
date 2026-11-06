using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.AddProductImage;

public sealed class AddProductImageCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<AddProductImageCommand, Guid>
{
    public async Task<Guid> Handle(AddProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var imageId = product.AddImage(
            request.StorageKey,
            request.AltText,
            request.DisplayOrder,
            request.IsPrimary,
            request.VariantId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return imageId;
    }
}
