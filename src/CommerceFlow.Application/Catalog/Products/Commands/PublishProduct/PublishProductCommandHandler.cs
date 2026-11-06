using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.PublishProduct;

public sealed class PublishProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<PublishProductCommand>
{
    public async Task Handle(PublishProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.Publish();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
