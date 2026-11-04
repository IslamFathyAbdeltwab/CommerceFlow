using CommerceFlow.Abstraction.Catalog.Interfaces;
using CommerceFlow.Abstraction.Common.Interfaces;
using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository products,
    IBrandRepository brands,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await products.SlugExistsAsync(slug, cancellationToken: cancellationToken))
            throw new ConflictException($"A product with slug '{slug}' already exists.");

        if (request.BrandId.HasValue &&
            !await brands.ExistsAsync(request.BrandId.Value, cancellationToken))
            throw new NotFoundException("Brand", request.BrandId.Value);

        var categoryIds = request.CategoryIds?.Distinct().ToList() ?? [];
        if (categoryIds.Count > 0 &&
            !await categories.AllExistAsync(categoryIds, cancellationToken))
            throw new NotFoundException("Category", string.Join(", ", categoryIds));

        var product = Product.Create(
            request.Name,
            request.Slug,
            request.Description,
            request.BrandId);

        foreach (var categoryId in categoryIds)
            product.AddCategory(categoryId);

        await products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}