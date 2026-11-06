using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await categories.SlugExistsAsync(slug, excludeCategoryId: request.CategoryId, cancellationToken: cancellationToken))
            throw new ConflictException($"A category with slug '{slug}' already exists.");

        if (request.ParentCategoryId.HasValue)
        {
            if (!await categories.ExistsAsync(request.ParentCategoryId.Value, cancellationToken))
                throw new NotFoundException("Category", request.ParentCategoryId.Value);

            if (await categories.IsDescendantAsync(request.ParentCategoryId.Value, request.CategoryId, cancellationToken))
                throw new ConflictException("A category cannot be moved under its own descendant.");
        }

        category.UpdateDetails(
            request.Name,
            request.Slug,
            request.Description,
            request.DisplayOrder,
            request.ParentCategoryId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
