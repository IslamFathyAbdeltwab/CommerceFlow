using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.ArchiveProduct;

public sealed class ArchiveProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<ArchiveProductCommand>
{
    public async Task Handle(ArchiveProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.Archive();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
