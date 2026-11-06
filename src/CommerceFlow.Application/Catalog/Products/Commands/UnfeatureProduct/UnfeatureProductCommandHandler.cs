using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.UnfeatureProduct;

public sealed class UnfeatureProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<UnfeatureProductCommand>
{
    public async Task Handle(UnfeatureProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.Unfeature();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
