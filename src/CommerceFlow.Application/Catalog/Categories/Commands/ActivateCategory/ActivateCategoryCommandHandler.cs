using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Categories.Commands.ActivateCategory;

public sealed class ActivateCategoryCommandHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork) : IRequestHandler<ActivateCategoryCommand>
{
    public async Task Handle(ActivateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        category.Activate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
