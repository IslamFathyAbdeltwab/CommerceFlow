using CommerceFlow.Domain.Entities.Notifications;
using FluentValidation;

namespace CommerceFlow.Application.Notifications.Commands.FailNotification;

public sealed class FailNotificationCommandValidator : AbstractValidator<FailNotificationCommand>
{
    public FailNotificationCommandValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();

        // a failure report must say why - the domain repeats this as its
        // safety net (shape-before-transition, same split as all modules)
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(Notification.FailureReasonMaxLength);
    }
}
