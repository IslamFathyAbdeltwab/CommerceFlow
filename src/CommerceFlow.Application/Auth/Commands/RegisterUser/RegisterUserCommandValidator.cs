using FluentValidation;

namespace CommerceFlow.Application.Auth.Commands.RegisterUser;

// Shape only: the email must look like an email, the name must fit the
// column, the password must meet the minimum policy. Whether the account
// already exists (409) and how the password is hashed are the handler's job.
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
