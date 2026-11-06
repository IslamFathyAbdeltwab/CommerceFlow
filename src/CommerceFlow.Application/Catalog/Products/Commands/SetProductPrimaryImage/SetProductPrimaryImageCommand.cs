using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.SetProductPrimaryImage;

public sealed record SetProductPrimaryImageCommand(Guid ProductId, Guid ImageId) : IRequest;
