using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await categories.SlugExistsAsync(slug, cancellationToken: cancellationToken))
            throw new ConflictException($"A category with slug '{slug}' already exists.");

        if (request.ParentCategoryId.HasValue &&
            !await categories.ExistsAsync(request.ParentCategoryId.Value, cancellationToken))
            throw new NotFoundException("Category", request.ParentCategoryId.Value);

        var category = Category.Create(
            request.Name,
            request.Slug,
            request.Description,
            request.DisplayOrder,
            request.ParentCategoryId);

        await categories.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
