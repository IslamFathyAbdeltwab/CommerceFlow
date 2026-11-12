using FluentValidation;

namespace CommerceFlow.Application.Addresses.Commands.DeleteAddress;

public sealed class DeleteAddressCommandValidator : AbstractValidator<DeleteAddressCommand>
{
    public DeleteAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
    }
}
