using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.RemoveProductCategory;

public sealed class RemoveProductCategoryCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveProductCategoryCommand>
{
    public async Task Handle(RemoveProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.RemoveCategory(request.CategoryId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
