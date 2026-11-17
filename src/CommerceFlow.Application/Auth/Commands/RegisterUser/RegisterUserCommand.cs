using MediatR;

namespace CommerceFlow.Application.Auth.Commands.RegisterUser;

// Creates an account and returns its id. The client then logs in for a
// token, so registration never hands out credentials by accident and the
// 201 carries exactly what a subsequent login needs: the account's id.
public sealed record RegisterUserCommand(
    string Email,
    string FullName,
    string Password) : IRequest<Guid>;
