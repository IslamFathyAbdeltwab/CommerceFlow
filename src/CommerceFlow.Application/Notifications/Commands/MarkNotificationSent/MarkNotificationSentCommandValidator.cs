using FluentValidation;

namespace CommerceFlow.Application.Notifications.Commands.MarkNotificationSent;

public sealed class MarkNotificationSentCommandValidator : AbstractValidator<MarkNotificationSentCommand>
{
    public MarkNotificationSentCommandValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
