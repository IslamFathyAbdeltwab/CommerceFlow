using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.AddProductCategory;

public sealed class AddProductCategoryCommandHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IRequestHandler<AddProductCategoryCommand>
{
    public async Task Handle(AddProductCategoryCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (!await categories.ExistsAsync(request.CategoryId, cancellationToken))
            throw new NotFoundException("Category", request.CategoryId);

        product.AddCategory(request.CategoryId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
