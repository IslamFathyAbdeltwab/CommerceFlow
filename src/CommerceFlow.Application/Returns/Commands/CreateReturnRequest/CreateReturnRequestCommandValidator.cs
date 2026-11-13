using CommerceFlow.Domain.Entities.Returns;
using FluentValidation;

namespace CommerceFlow.Application.Returns.Commands.CreateReturnRequest;

public sealed class CreateReturnRequestCommandValidator : AbstractValidator<CreateReturnRequestCommand>
{
    public CreateReturnRequestCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).IsInEnum();

        RuleFor(x => x.Notes)
            .MaximumLength(ReturnRequest.NotesMaxLength);

        // shape only: whether the quantities are returnable is business logic
        // and lives in the handler against the order lines
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductVariantId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}
