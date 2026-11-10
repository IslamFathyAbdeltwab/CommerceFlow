using FluentValidation;

namespace CommerceFlow.Application.Orders.Commands.SetShippingFee;

public sealed class SetShippingFeeCommandValidator : AbstractValidator<SetShippingFeeCommand>
{
    public SetShippingFeeCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.ShippingFee).GreaterThanOrEqualTo(0);
    }
}
