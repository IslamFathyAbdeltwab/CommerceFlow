using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.FeatureProduct;

public sealed record FeatureProductCommand(Guid ProductId) : IRequest;
