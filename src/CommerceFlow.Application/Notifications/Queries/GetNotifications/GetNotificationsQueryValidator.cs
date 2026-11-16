using CommerceFlow.Domain.Entities.Notifications;
using FluentValidation;

namespace CommerceFlow.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        // null passes (filter simply off); an empty Guid is shape nonsense.
        // NotEmpty looks right here but never fires on a Guid? - its default
        // comparison is null, not Guid.Empty - so compare explicitly
        RuleFor(x => x.RecipientId)
            .Must(id => id != Guid.Empty)
            .When(x => x.RecipientId.HasValue);
    }
}
