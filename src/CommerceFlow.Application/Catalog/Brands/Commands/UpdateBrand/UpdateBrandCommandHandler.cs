using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Commands.UpdateBrand;

public sealed class UpdateBrandCommandHandler(
    IBrandRepository brands,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateBrandCommand>
{
    public async Task Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await brands.GetByIdAsync(request.BrandId, cancellationToken)
            ?? throw new NotFoundException(nameof(Brand), request.BrandId);

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await brands.SlugExistsAsync(slug, excludeBrandId: request.BrandId, cancellationToken: cancellationToken))
            throw new ConflictException($"A brand with slug '{slug}' already exists.");

        brand.UpdateDetails(request.Name, request.Slug, request.Description, request.LogoUrl);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
