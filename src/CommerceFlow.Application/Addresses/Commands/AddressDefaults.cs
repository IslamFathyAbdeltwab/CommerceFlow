using CommerceFlow.Application.Addresses.Interfaces;

namespace CommerceFlow.Application.Addresses.Commands;

// Shared by CreateAddress and SetDefaultAddress: "one default per customer"
// means the previous default must be cleared before the new one is saved
// (both land in the same SaveChanges, which updates before it inserts).
internal static class AddressDefaults
{
    public static async Task ClearPreviousAsync(
        ICustomerAddressRepository addresses,
        Guid customerId,
        Guid keepAddressId,
        CancellationToken cancellationToken)
    {
        var currentDefault = await addresses.GetDefaultAsync(customerId, cancellationToken);
        if (currentDefault is not null && currentDefault.Id != keepAddressId)
            currentDefault.ClearDefault(); // domain-owned state change
    }
}
