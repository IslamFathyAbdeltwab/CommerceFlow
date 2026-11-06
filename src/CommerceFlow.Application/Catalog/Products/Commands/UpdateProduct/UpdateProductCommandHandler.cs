using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IProductRepository products,
    IBrandRepository brands,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await products.SlugExistsAsync(slug, excludeProductId: request.ProductId, cancellationToken: cancellationToken))
            throw new ConflictException($"A product with slug '{slug}' already exists.");

        if (request.BrandId.HasValue &&
            !await brands.ExistsAsync(request.BrandId.Value, cancellationToken))
            throw new NotFoundException("Brand", request.BrandId.Value);

        product.UpdateDetails(request.Name, request.Slug, request.Description, request.BrandId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
