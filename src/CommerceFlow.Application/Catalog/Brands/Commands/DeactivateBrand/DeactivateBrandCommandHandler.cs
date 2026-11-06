using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Commands.DeactivateBrand;

public sealed class DeactivateBrandCommandHandler(
    IBrandRepository brands,
    IUnitOfWork unitOfWork) : IRequestHandler<DeactivateBrandCommand>
{
    public async Task Handle(DeactivateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await brands.GetByIdAsync(request.BrandId, cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), request.BrandId);

        brand.Deactivate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
