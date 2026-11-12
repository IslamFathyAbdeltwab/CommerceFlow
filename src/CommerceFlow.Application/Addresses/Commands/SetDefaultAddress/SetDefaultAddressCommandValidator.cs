using FluentValidation;

namespace CommerceFlow.Application.Addresses.Commands.SetDefaultAddress;

public sealed class SetDefaultAddressCommandValidator : AbstractValidator<SetDefaultAddressCommand>
{
    public SetDefaultAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
    }
}
