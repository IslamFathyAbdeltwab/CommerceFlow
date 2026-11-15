using CommerceFlow.Domain.Enums;
using MediatR;

namespace CommerceFlow.Application.Promotions.Commands.CreatePromotion;

// Creates the campaign as Draft - publishing is an explicit Activate (same
// split as Product's Draft/Published). Returns the promotion id. Code
// uniqueness is pre-checked by the handler against IPromotionRepository.
public sealed record CreatePromotionCommand(
    string Code,
    string Name,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartAtUtc,
    string? Description = null,
    DateTimeOffset? EndAtUtc = null,
    decimal? MinOrderAmount = null,
    int? MaxUsesTotal = null,
    int? MaxUsesPerCustomer = null) : IRequest<Guid>;
