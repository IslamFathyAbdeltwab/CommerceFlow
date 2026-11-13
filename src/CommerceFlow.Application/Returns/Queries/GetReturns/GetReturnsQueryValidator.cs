using FluentValidation;

namespace CommerceFlow.Application.Returns.Queries.GetReturns;

public sealed class GetReturnsQueryValidator : AbstractValidator<GetReturnsQuery>
{
    public GetReturnsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
