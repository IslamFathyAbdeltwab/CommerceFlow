namespace CommerceFlow.Application.Auth.Queries.Dtos;

// The account as clients see it - never the password hash, never internal
// timestamps they have no use for.
public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role);

// /api/Auth/me: the same shape plus the last successful sign-in, which is
// only meaningful to the account itself.
public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    DateTimeOffset? LastLoginAtUtc);
