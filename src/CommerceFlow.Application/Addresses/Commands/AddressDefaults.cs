using CommerceFlow.Application.Addresses.Interfaces;

namespace CommerceFlow.Application.Addresses.Commands;

// Shared by CreateAddress and SetDefaultAddress: "one default per customer"
// means the previous default must be cleared before the new one is written.
// This is an immediate statement of its own (ClearDefaultAsync), NOT part of
// the upcoming SaveChanges: SQL Server validates the filtered unique index
// per statement, so two UPDATEs in one EF batch would collide no matter what
// order EF sends them in. A separate statement always completes first.
internal static class AddressDefaults
{
    public static Task ClearPreviousAsync(
        ICustomerAddressRepository addresses,
        Guid customerId,
        Guid keepAddressId,
        CancellationToken cancellationToken)
        => addresses.ClearDefaultAsync(customerId, keepAddressId, cancellationToken);
}
