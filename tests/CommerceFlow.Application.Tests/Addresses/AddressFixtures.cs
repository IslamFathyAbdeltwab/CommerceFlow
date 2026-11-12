using CommerceFlow.Application.Addresses.Commands.CreateAddress;
using CommerceFlow.Application.Addresses.Commands.UpdateAddress;
using CommerceFlow.Domain.Entities.Addresses;

namespace CommerceFlow.Application.Tests.Addresses;

// Shared factories for building addresses and commands in application tests.
internal static class AddressFixtures
{
    public static readonly Guid CustomerId = Guid.Parse("44441111-4444-4444-4444-444444444444");

    public static CustomerAddress CreateAddress(
        Guid? customerId = null,
        bool isDefault = false) =>
        CustomerAddress.Create(
            customerId ?? CustomerId,
            "John Doe",
            "1 Main Street",
            "Cairo",
            "12345",
            "EG",
            line2: "Apt 4B",
            state: "CA",
            phoneNumber: "01000000000",
            isDefault: isDefault);

    public static CreateAddressCommand ValidCreateCommand(Guid? customerId = null) =>
        new(
            customerId ?? CustomerId,
            "John Doe",
            "1 Main Street",
            "Cairo",
            "12345",
            "EG",
            Line2: "Apt 4B",
            State: "CA",
            PhoneNumber: "01000000000");

    public static UpdateAddressCommand ValidUpdateCommand(Guid addressId) =>
        new(
            addressId,
            "Jane Doe",
            "2 Second Street",
            "Alexandria",
            "23456",
            "EG",
            Line2: "Floor 3",
            State: "AL",
            PhoneNumber: "01111111111");
}
