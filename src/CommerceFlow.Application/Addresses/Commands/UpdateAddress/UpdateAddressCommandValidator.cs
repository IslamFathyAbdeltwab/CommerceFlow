using CommerceFlow.Domain.Entities.Addresses;
using FluentValidation;

namespace CommerceFlow.Application.Addresses.Commands.UpdateAddress;

public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(CustomerAddress.FullNameMaxLength);
        RuleFor(x => x.Line1)
            .NotEmpty()
            .MaximumLength(CustomerAddress.LineMaxLength);
        RuleFor(x => x.Line2)
            .MaximumLength(CustomerAddress.LineMaxLength);
        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(CustomerAddress.CityMaxLength);
        RuleFor(x => x.State)
            .MaximumLength(CustomerAddress.StateMaxLength);
        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .MaximumLength(CustomerAddress.PostalCodeMaxLength);
        RuleFor(x => x.Country)
            .NotEmpty()
            .MaximumLength(CustomerAddress.CountryMaxLength);
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(CustomerAddress.PhoneNumberMaxLength);
    }
}
