using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.RemoveProductCategory;

public sealed record RemoveProductCategoryCommand(Guid ProductId, Guid CategoryId) : IRequest;
