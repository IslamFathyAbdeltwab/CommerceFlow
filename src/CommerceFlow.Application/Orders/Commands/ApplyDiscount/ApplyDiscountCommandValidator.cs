using FluentValidation;

namespace CommerceFlow.Application.Orders.Commands.ApplyDiscount;

public sealed class ApplyDiscountCommandValidator : AbstractValidator<ApplyDiscountCommand>
{
    public ApplyDiscountCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
    }
}
