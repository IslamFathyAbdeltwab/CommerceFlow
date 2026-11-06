using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.SetProductPrimaryImage;

public sealed class SetProductPrimaryImageCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<SetProductPrimaryImageCommand>
{
    public async Task Handle(SetProductPrimaryImageCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.SetPrimaryImage(request.ImageId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
