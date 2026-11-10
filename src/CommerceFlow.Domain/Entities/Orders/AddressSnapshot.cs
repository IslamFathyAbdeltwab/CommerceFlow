using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Entities.Orders;

// Immutable value object: the shipping address as it was at checkout time.
// Stored as owned columns on the Order row (configured in Infrastructure),
// so later edits to a customer's saved address never rewrite old orders.
public class AddressSnapshot
{
    public const int FullNameMaxLength = 200;
    public const int LineMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int StateMaxLength = 100;
    public const int PostalCodeMaxLength = 20;
    public const int CountryMaxLength = 100;
    public const int PhoneNumberMaxLength = 30;

    public string FullName { get; private set; } = string.Empty;
    public string Line1 { get; private set; } = string.Empty;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? State { get; private set; }
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }

    private AddressSnapshot() { } // required by EF Core

    private AddressSnapshot(
        string fullName,
        string line1,
        string? line2,
        string city,
        string? state,
        string postalCode,
        string country,
        string? phoneNumber)
    {
        FullName = fullName;
        Line1 = line1;
        Line2 = line2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        PhoneNumber = phoneNumber;
    }

    public static AddressSnapshot Create(
        string fullName,
        string line1,
        string city,
        string postalCode,
        string country,
        string? line2 = null,
        string? state = null,
        string? phoneNumber = null)
    {
        return new AddressSnapshot(
            Required(fullName, "Full name", FullNameMaxLength),
            Required(line1, "Address line", LineMaxLength),
            Optional(line2, "Address line 2", LineMaxLength),
            Required(city, "City", CityMaxLength),
            Optional(state, "State", StateMaxLength),
            Required(postalCode, "Postal code", PostalCodeMaxLength),
            Required(country, "Country", CountryMaxLength),
            Optional(phoneNumber, "Phone number", PhoneNumberMaxLength));
    }

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
}
