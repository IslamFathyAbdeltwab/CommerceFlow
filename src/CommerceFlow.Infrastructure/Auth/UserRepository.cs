using CommerceFlow.Application.Auth.Interfaces;
using CommerceFlow.Domain.Entities.Users;
using CommerceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceFlow.Infrastructure.Auth;

internal sealed class UserRepository(CommerceFlowDbContext dbContext) : IUserRepository
{
    // Tracked on purpose: login mutates what it loads (last-login stamp) and
    // saves through the unit of work, like the other modules.
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    // Emails are stored normalized (User.NormalizeEmail), so an exact match
    // is the whole lookup - no collation games, no case-sensitivity traps.
    public async Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await dbContext.Users.AddAsync(user, cancellationToken);
}
