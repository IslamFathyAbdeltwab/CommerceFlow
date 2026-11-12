using FluentValidation;

namespace CommerceFlow.Application.Payments.Queries.GetPayments;

public sealed class GetPaymentsQueryValidator : AbstractValidator<GetPaymentsQuery>
{
    public GetPaymentsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
