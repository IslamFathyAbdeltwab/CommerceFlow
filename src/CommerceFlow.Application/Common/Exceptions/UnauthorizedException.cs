namespace CommerceFlow.Application.Common.Exceptions;

// Credentials missing or wrong (401). Deliberately thrown with the same
// message for "no such account" and "wrong password" so responses cannot be
// mined to enumerate which emails exist - the boundary answers with one
// story, either way.
public sealed class UnauthorizedException(string message) : Exception(message);
