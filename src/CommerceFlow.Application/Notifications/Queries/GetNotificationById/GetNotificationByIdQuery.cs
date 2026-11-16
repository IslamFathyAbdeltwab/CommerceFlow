using MediatR;

namespace CommerceFlow.Application.Notifications.Queries.GetNotificationById;

// null = not found (the controller maps it to 404, same as GetPromotionById).
public sealed record GetNotificationByIdQuery(Guid NotificationId) : IRequest<NotificationDto?>;
