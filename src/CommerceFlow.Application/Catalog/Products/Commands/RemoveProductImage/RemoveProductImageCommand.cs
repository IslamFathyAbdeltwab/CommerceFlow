using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.RemoveProductImage;

public sealed record RemoveProductImageCommand(Guid ProductId, Guid ImageId) : IRequest;
