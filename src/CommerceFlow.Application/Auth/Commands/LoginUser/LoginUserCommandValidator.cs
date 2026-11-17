using FluentValidation;

namespace CommerceFlow.Application.Auth.Commands.LoginUser;

// Shape only. The password policy (length, letter, digit) is enforced when
// the account is created - re-checking it at login would only tell an
// attacker which rule their guess broke.
public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
