using CommerceFlow.Application.Notifications.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Notifications.Queries.GetNotificationById;

public sealed class GetNotificationByIdQueryHandler(INotificationRepository notifications)
    : IRequestHandler<GetNotificationByIdQuery, NotificationDto?>
{
    public async Task<NotificationDto?> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(request.NotificationId, cancellationToken);

        return notification is null ? null : NotificationMappings.ToDto(notification);
    }
}
