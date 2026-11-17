using CommerceFlow.Application.Auth.Interfaces;
using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Application.Common.Interfaces;
using CommerceFlow.Domain.Entities.Users;
using MediatR;

namespace CommerceFlow.Application.Auth.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterUserCommand, Guid>
{
    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);

        // Pre-check before the state moves: a duplicate account is a 409, not
        // a unique-index blowup. The unique index in the database stays as
        // the race-proof safety net behind this check.
        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
            throw new ConflictException($"An account with email '{email}' already exists.");

        // The password goes out as plaintext exactly once - here - and what
        // reaches the repository is only ever its hash.
        var user = User.Create(
            email,
            request.FullName,
            passwordHasher.Hash(request.Password));

        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
