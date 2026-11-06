using MediatR;

namespace CommerceFlow.Application.Catalog.Brands.Commands.DeactivateBrand;

public sealed record DeactivateBrandCommand(Guid BrandId) : IRequest;
