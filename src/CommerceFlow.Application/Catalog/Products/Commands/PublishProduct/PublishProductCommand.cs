using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.PublishProduct;

public sealed record PublishProductCommand(Guid ProductId) : IRequest;
