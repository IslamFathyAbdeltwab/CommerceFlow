using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.UnfeatureProduct;

public sealed record UnfeatureProductCommand(Guid ProductId) : IRequest;
