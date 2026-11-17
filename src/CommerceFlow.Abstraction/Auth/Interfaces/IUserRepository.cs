using CommerceFlow.Domain.Entities.Users;

namespace CommerceFlow.Application.Auth.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Emails are normalized at the domain boundary (User.NormalizeEmail),
    // so callers must pass an already-normalized value - the stored column
    // and the lookup then speak the same language.
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    // No Delete: accounts are the anchor of orders, reviews and history.
}
