using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Commands.CreateBrand;

public sealed record CreateBrandCommand(
    string Name,
    string Slug,
    string? Description = null,
    string? LogoUrl = null) : IRequest<Guid>;
