using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.ArchiveProduct;

public sealed record ArchiveProductCommand(Guid ProductId) : IRequest;
