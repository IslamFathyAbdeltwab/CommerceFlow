using CommerceFlow.Application.Auth.Queries.Dtos;
using MediatR;

namespace CommerceFlow.Application.Auth.Commands.LoginUser;

// Exchanges credentials for a signed access token plus the identity it
// carries. Unknown email and wrong password answer with the same 401 and
// the same message, so the endpoint cannot be used to discover accounts.
public sealed record LoginUserCommand(string Email, string Password) : IRequest<LoginResultDto>;
