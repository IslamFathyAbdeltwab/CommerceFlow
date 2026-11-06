using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Commands.ActivateBrand;

public sealed record ActivateBrandCommand(Guid BrandId) : IRequest;
