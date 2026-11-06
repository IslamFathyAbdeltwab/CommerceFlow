using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.FeatureProduct;

public sealed class FeatureProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<FeatureProductCommand>
{
    public async Task Handle(FeatureProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.Feature();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
