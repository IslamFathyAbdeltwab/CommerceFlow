using CommerceFlow.Application.Common.Interfaces;

namespace CommerceFlow.Infrastructure.Persistence;

// Unit of Work pattern: one SaveChanges call commits the whole set of repository
// changes in a single transaction. Repositories only track entities, never save.
internal sealed class UnitOfWork(CommerceFlowDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
