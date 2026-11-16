using CommerceFlow.Application.Catalog.Products.Queries;
using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Notifications.Queries.GetNotifications;

// RecipientId = the personal feed, Status = the admin's "what is stuck" view;
// both off lists everything with the usual paging (same call as GetPromotions).
public sealed record GetNotificationsQuery(
    Guid? RecipientId = null,
    NotificationStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<NotificationDto>>;
