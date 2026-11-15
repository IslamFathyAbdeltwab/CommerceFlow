using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Application.Promotions.Interfaces;
using CommerceFlow.Domain.Entities.Promotions;
using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.CreatePromotion;

public sealed class CreatePromotionCommandHandler(
    IPromotionRepository promotions,
    IUnitOfWork unitOfWork) : IRequestHandler<CreatePromotionCommand, Guid>
{
    public async Task<Guid> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();

        // uniqueness pre-check before anything is created (conflict -> 409);
        // the database unique index is the backstop (same split as Review)
        var existing = await promotions.GetByCodeAsync(code, cancellationToken);
        if (existing is not null)
            throw new ConflictException(
                $"A promotion with code '{code}' already exists.");

        var promotion = Promotion.Create(
            request.Code,
            request.Name,
            request.DiscountType,
            request.DiscountValue,
            request.StartAtUtc,
            request.Description,
            request.EndAtUtc,
            request.MinOrderAmount,
            request.MaxUsesTotal,
            request.MaxUsesPerCustomer);

        await promotions.AddAsync(promotion, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return promotion.Id;
    }
}
