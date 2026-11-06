using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Commands.CreateBrand;

public sealed class CreateBrandCommandHandler(
    IBrandRepository brands,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBrandCommand, Guid>
{
    public async Task<Guid> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await brands.SlugExistsAsync(slug, cancellationToken: cancellationToken))
            throw new ConflictException($"A brand with slug '{slug}' already exists.");

        var brand = Brand.Create(request.Name, request.Slug, request.Description, request.LogoUrl);

        await brands.AddAsync(brand, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return brand.Id;
    }
}
