using CommerceFlow.Domain.Entities.Notifications;
using FluentValidation;

namespace CommerceFlow.Application.Notifications.Commands.SendNotification;

public sealed class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(x => x.RecipientId).NotEmpty();

        RuleFor(x => x.Channel).IsInEnum();

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(Notification.SubjectMaxLength);

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(Notification.BodyMaxLength);

        // null passes (MaximumLength allows it): a missing address for a
        // sender channel, the related-entity pairing and the unknown-enum
        // case are business rules and live in the domain
        RuleFor(x => x.RecipientAddress)
            .MaximumLength(Notification.AddressMaxLength);

        RuleFor(x => x.RelatedEntityType)
            .MaximumLength(Notification.RelatedEntityTypeMaxLength);
    }
}
