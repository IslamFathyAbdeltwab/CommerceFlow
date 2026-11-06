using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.AddProductCategory;

public sealed record AddProductCategoryCommand(Guid ProductId, Guid CategoryId) : IRequest;
