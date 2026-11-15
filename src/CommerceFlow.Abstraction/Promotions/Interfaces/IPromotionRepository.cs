using CommerceFlow.Domain.Entities.Promotions;
using CommerceFlow.Domain.Enums;

namespace CommerceFlow.Application.Promotions.Interfaces;

public interface IPromotionRepository
{
    // Must load Usages: the domain counts per-customer redemptions from that
    // collection when enforcing limits (same reason ReturnRequestRepository
    // includes Items).
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Case-insensitive exact lookup: the create handler uses it for the
    // uniqueness pre-check (the database unique index is the backstop, same
    // split Review uses for its (product, customer) pair).
    Task<Promotion?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    // Code is a case-insensitive contains-filter for the admin list.
    Task<IReadOnlyList<Promotion>> GetAllAsync(
        string? code,
        PromotionStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        string? code,
        PromotionStatus? status,
        CancellationToken cancellationToken = default);

    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);

    // No Delete: campaigns are ended (Expire), never removed - the usage
    // rows are an audit trail. Updates ride EF change tracking, like the
    // other repositories.
}
