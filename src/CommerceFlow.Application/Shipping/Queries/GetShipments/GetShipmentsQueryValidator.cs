using CommerceFlow.Application.Catalog.Products.Queries;
using FluentValidation;

namespace CommerceFlow.Application.Shipping.Queries.GetShipments;

public sealed class GetShipmentsQueryValidator : AbstractValidator<GetShipmentsQuery>
{
    public GetShipmentsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
