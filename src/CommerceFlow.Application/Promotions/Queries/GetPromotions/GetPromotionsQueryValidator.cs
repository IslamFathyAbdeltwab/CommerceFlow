using CommerceFlow.Domain.Entities.Promotions;
using FluentValidation;

namespace CommerceFlow.Application.Promotions.Queries.GetPromotions;

public sealed class GetPromotionsQueryValidator : AbstractValidator<GetPromotionsQuery>
{
    public GetPromotionsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        // null/empty passes (filter simply off), same shape rule as other lists
        RuleFor(x => x.Code).MaximumLength(Promotion.CodeMaxLength);
    }
}
