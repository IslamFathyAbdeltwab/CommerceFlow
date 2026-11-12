using CommerceFlow.Domain.Entities.Payments;
using FluentValidation;

namespace CommerceFlow.Application.Payments.Commands.CreatePayment;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();

        RuleFor(x => x.Amount).GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(Payment.CurrencyLength);
        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(Payment.ProviderMaxLength);
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(Payment.IdempotencyKeyMaxLength);
    }
}
