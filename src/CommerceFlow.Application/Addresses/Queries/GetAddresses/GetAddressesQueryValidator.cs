using FluentValidation;

namespace CommerceFlow.Application.Addresses.Queries.GetAddresses;

public sealed class GetAddressesQueryValidator : AbstractValidator<GetAddressesQuery>
{
    public GetAddressesQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
