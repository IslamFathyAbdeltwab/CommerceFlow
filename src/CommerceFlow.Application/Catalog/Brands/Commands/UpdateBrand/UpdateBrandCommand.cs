using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Commands.UpdateBrand;

public sealed record UpdateBrandCommand(
    Guid BrandId,
    string Name,
    string Slug,
    string? Description = null,
    string? LogoUrl = null) : IRequest;
