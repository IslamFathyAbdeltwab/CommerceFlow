using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Commands.DeactivateCategory;

public sealed class DeactivateCategoryCommandHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IRequestHandler<DeactivateCategoryCommand>
{
    public async Task Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        category.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
