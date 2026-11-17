using CommerceFlow.Application.Auth.Queries.Dtos;
using MediatR;

namespace CommerceFlow.Application.Auth.Queries.GetCurrentUser;

// Echoes the identity the verified token carries - the proof that the
// claims (sub, email, name, role) survived the round trip and the anchor
// for any client that needs "who am I" without decoding the JWT itself.
public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;
