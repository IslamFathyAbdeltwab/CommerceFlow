using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using MediatR;

namespace CommerceFlow.Application.Catalog.Products.Commands.ChangeVariantPrice;

public sealed class ChangeVariantPriceCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeVariantPriceCommand>
{
    public async Task Handle(ChangeVariantPriceCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        product.ChangeVariantPrice(request.VariantId, request.Price, request.CompareAtPrice);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
