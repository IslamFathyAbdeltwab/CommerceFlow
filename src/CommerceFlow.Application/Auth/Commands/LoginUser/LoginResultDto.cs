using CommerceFlow.Application.Auth.Queries.Dtos;

namespace CommerceFlow.Application.Auth.Commands.LoginUser;

// The full answer to a login: the credential to send on every later call,
// how long it lives, and who it belongs to.
public sealed record LoginResultDto(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    UserDto User);
