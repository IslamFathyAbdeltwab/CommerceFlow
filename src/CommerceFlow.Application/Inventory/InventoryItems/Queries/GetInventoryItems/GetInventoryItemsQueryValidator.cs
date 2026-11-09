using FluentValidation;

namespace CommerceFlow.Application.Inventory.InventoryItems.Queries.GetInventoryItems;

public sealed class GetInventoryItemsQueryValidator : AbstractValidator<GetInventoryItemsQuery>
{
    public GetInventoryItemsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
