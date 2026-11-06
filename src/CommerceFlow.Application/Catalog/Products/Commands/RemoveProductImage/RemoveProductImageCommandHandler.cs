using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.RemoveProductImage;

public sealed class RemoveProductImageCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveProductImageCommand>
{
    public async Task Handle(RemoveProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.RemoveImage(request.ImageId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
