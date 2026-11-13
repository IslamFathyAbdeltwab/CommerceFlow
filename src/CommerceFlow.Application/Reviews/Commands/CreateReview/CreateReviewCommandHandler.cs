using CommerceFlow.Application.Catalog.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Orders.Interfaces;
using CommerceFlow.Application.Reviews.Interfaces;
using CommerceFlow.Domain.Entities.Catalog;
using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Entities.Reviews;
using MediatR;

namespace CommerceFlow.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewCommandHandler(
    IProductReviewRepository reviews,
    IProductRepository products,
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateReviewCommand, Guid>
{
    public async Task<Guid> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        // pre-checks before anything is created (state conflicts -> 409)
        _ = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (request.OrderId is { } orderId)
        {
            var order = await orders.GetByIdAsync(orderId, cancellationToken)
                ?? throw new NotFoundException(nameof(Order), orderId);

            // the verified-purchase badge must prove THIS customer's purchase
            if (order.CustomerId != request.CustomerId)
                throw new ConflictException("Order does not belong to the customer.");
        }

        // one review per customer per product: a second submission would be
        // abuse, so it conflicts instead of silently replacing the first
        if (await reviews.GetByProductAndCustomerAsync(
                request.ProductId, request.CustomerId, cancellationToken) is not null)
            throw new ConflictException("Customer has already reviewed this product.");

        var review = ProductReview.Create(
            request.ProductId,
            request.CustomerId,
            request.Rating,
            request.Comment,
            request.Title,
            request.OrderId);

        await reviews.AddAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return review.Id;
    }
}
