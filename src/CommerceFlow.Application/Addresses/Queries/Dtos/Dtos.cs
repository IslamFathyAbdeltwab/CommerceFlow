using CommerceFlow.Domain.Entities.Addresses;

namespace CommerceFlow.Application.Addresses.Queries;

// One DTO for both detail and list reads: an address has no children,
// so a separate list-item type would add nothing.
public sealed record AddressDto(
    Guid Id,
    Guid CustomerId,
    string FullName,
    string Line1,
    string? Line2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    string? PhoneNumber,
    bool IsDefault,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

// Centralizes entity -> DTO mapping so the query handlers stay thin.
internal static class AddressMappings
{
    public static AddressDto ToDto(CustomerAddress address) => new(
        address.Id,
        address.CustomerId,
        address.FullName,
        address.Line1,
        address.Line2,
        address.City,
        address.State,
        address.PostalCode,
        address.Country,
        address.PhoneNumber,
        address.IsDefault,
        address.CreatedAtUtc,
        address.UpdatedAtUtc);
}
