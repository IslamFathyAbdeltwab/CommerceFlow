using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Commands.ActivateBrand;

public sealed class ActivateBrandCommandHandler(
    IBrandRepository brands,
    IUnitOfWork unitOfWork) : IRequestHandler<ActivateBrandCommand>
{
    public async Task Handle(ActivateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await brands.GetByIdAsync(request.BrandId, cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), request.BrandId);

        brand.Activate();

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
