using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Application.Notifications.Interfaces;
using MediatR;

namespace CommerceFlow.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(INotificationRepository notifications)
    : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await notifications.CountAsync(
            request.RecipientId,
            request.Status,
            cancellationToken);

        var list = await notifications.GetAllAsync(
            request.RecipientId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var items = list.Select(NotificationMappings.ToDto).ToList();

        return new PagedResult<NotificationDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
