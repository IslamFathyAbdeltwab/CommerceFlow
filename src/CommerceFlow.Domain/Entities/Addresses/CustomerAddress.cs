using CommerceFlow.Domain.Common;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Addresses;

// Aggregate root: one saved address in a customer's address book.
// At checkout the Order module copies these fields into an immutable
// AddressSnapshot, so later edits to a saved address never rewrite old
// orders. Field rules (trim, required, max lengths) deliberately mirror
// Orders.AddressSnapshot so both agree on the same shape.
public class CustomerAddress : AggregateRoot
{
    public const int FullNameMaxLength = 200;
    public const int LineMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int StateMaxLength = 100;
    public const int PostalCodeMaxLength = 20;
    public const int CountryMaxLength = 100;
    public const int PhoneNumberMaxLength = 30;

    public Guid CustomerId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Line1 { get; private set; } = string.Empty;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? State { get; private set; }
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public bool IsDefault { get; private set; }

    private CustomerAddress() { } // required by EF Core

    private CustomerAddress(
        Guid customerId,
        string fullName,
        string line1,
        string? line2,
        string city,
        string? state,
        string postalCode,
        string country,
        string? phoneNumber,
        bool isDefault)
    {
        CustomerId = customerId;
        FullName = fullName;
        Line1 = line1;
        Line2 = line2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        PhoneNumber = phoneNumber;
        IsDefault = isDefault;
        CreatedAtUtc = UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static CustomerAddress Create(
        Guid customerId,
        string fullName,
        string line1,
        string city,
        string postalCode,
        string country,
        string? line2 = null,
        string? state = null,
        string? phoneNumber = null,
        bool isDefault = false)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID is required.");

        return new CustomerAddress(
            customerId,
            Required(fullName, "Full name", FullNameMaxLength),
            Required(line1, "Address line", LineMaxLength),
            Optional(line2, "Address line 2", LineMaxLength),
            Required(city, "City", CityMaxLength),
            Optional(state, "State", StateMaxLength),
            Required(postalCode, "Postal code", PostalCodeMaxLength),
            Required(country, "Country", CountryMaxLength),
            Optional(phoneNumber, "Phone number", PhoneNumberMaxLength),
            isDefault);
    }

    // Edits rewrite the address in place (validate first, then assign, so a
    // rejected update never leaves the entity half-changed).
    public void Update(
        string fullName,
        string line1,
        string city,
        string postalCode,
        string country,
        string? line2 = null,
        string? state = null,
        string? phoneNumber = null)
    {
        var newFullName = Required(fullName, "Full name", FullNameMaxLength);
        var newLine1 = Required(line1, "Address line", LineMaxLength);
        var newLine2 = Optional(line2, "Address line 2", LineMaxLength);
        var newCity = Required(city, "City", CityMaxLength);
        var newState = Optional(state, "State", StateMaxLength);
        var newPostalCode = Required(postalCode, "Postal code", PostalCodeMaxLength);
        var newCountry = Required(country, "Country", CountryMaxLength);
        var newPhoneNumber = Optional(phoneNumber, "Phone number", PhoneNumberMaxLength);

        FullName = newFullName;
        Line1 = newLine1;
        Line2 = newLine2;
        City = newCity;
        State = newState;
        PostalCode = newPostalCode;
        Country = newCountry;
        PhoneNumber = newPhoneNumber;
        Touch();
    }

    // One default address per customer: "only one across the book" is a
    // cross-address invariant, so the Application layer clears the previous
    // default (and Infrastructure adds a filtered unique index as a safety
    // net). The domain only owns this address's own state - both methods
    // are idempotent.
    public void SetDefault()
    {
        if (IsDefault)
            return;

        IsDefault = true;
        Touch();
    }

    public void ClearDefault()
    {
        if (!IsDefault)
            return;

        IsDefault = false;
        Touch();
    }

    // ---------- Validation (mirrors Orders.AddressSnapshot) ----------

    private static string Required(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }

    private static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");

        return trimmed;
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
