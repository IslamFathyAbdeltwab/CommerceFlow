using CommerceFlow.Domain.Entities.Addresses;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Addresses;

public class CustomerAddressTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static CustomerAddress NewAddress(bool isDefault = false) =>
        CustomerAddress.Create(
            CustomerId,
            "John Doe",
            "1 Main Street",
            "Cairo",
            "12345",
            "EG",
            line2: "Apt 4B",
            state: "CA",
            phoneNumber: "01000000000",
            isDefault: isDefault);

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_CreatesAddressForCustomer()
    {
        var address = NewAddress();

        Assert.Equal(CustomerId, address.CustomerId);
        Assert.Equal("John Doe", address.FullName);
        Assert.Equal("1 Main Street", address.Line1);
        Assert.Equal("Apt 4B", address.Line2);
        Assert.Equal("Cairo", address.City);
        Assert.Equal("CA", address.State);
        Assert.Equal("12345", address.PostalCode);
        Assert.Equal("EG", address.Country);
        Assert.Equal("01000000000", address.PhoneNumber);
        Assert.False(address.IsDefault);
        Assert.NotEqual(Guid.Empty, address.Id);
    }

    [Fact]
    public void Create_WithDefaultFlag_CreatesDefaultAddress()
    {
        var address = NewAddress(isDefault: true);

        Assert.True(address.IsDefault);
    }

    [Fact]
    public void Create_SetsAuditTimestamps()
    {
        var address = NewAddress();

        Assert.NotEqual(default, address.CreatedAtUtc);
        Assert.NotEqual(default, address.UpdatedAtUtc);
        Assert.True(address.UpdatedAtUtc >= address.CreatedAtUtc);
    }

    [Fact]
    public void Create_EmptyCustomerId_Throws()
    {
        Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                Guid.Empty, "John Doe", "1 Main Street", "Cairo", "12345", "EG"));
    }

    [Fact]
    public void Create_TrimsRequiredFields()
    {
        var address = CustomerAddress.Create(
            CustomerId, "  John Doe  ", " 1 Main Street ", " Cairo ", " 12345 ", " EG ");

        Assert.Equal("John Doe", address.FullName);
        Assert.Equal("1 Main Street", address.Line1);
        Assert.Equal("Cairo", address.City);
        Assert.Equal("12345", address.PostalCode);
        Assert.Equal("EG", address.Country);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_OptionalFieldMissingOrWhitespace_BecomesNull(string? value)
    {
        var address = CustomerAddress.Create(
            CustomerId, "John Doe", "1 Main Street", "Cairo", "12345", "EG",
            line2: value, state: value, phoneNumber: value);

        Assert.Null(address.Line2);
        Assert.Null(address.State);
        Assert.Null(address.PhoneNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_MissingFullName_Throws(string value)
    {
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                CustomerId, value, "1 Main Street", "Cairo", "12345", "EG"));

        Assert.Equal("Full name is required.", exception.Message);
    }

    [Fact]
    public void Create_MissingLine1_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                CustomerId, "John Doe", "  ", "Cairo", "12345", "EG"));

        Assert.Equal("Address line is required.", exception.Message);
    }

    [Fact]
    public void Create_MissingCity_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                CustomerId, "John Doe", "1 Main Street", "", "12345", "EG"));

        Assert.Equal("City is required.", exception.Message);
    }

    [Fact]
    public void Create_MissingPostalCode_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                CustomerId, "John Doe", "1 Main Street", "Cairo", "  ", "EG"));

        Assert.Equal("Postal code is required.", exception.Message);
    }

    [Fact]
    public void Create_MissingCountry_Throws()
    {
        // null! on purpose: JSON binding can deliver null for a non-nullable
        // field, and Required() must reject it defensively.
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                CustomerId, "John Doe", "1 Main Street", "Cairo", "12345", null!));

        Assert.Equal("Country is required.", exception.Message);
    }

    [Fact]
    public void Create_FullNameTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                CustomerId,
                new string('x', CustomerAddress.FullNameMaxLength + 1),
                "1 Main Street", "Cairo", "12345", "EG"));

        Assert.Equal("Full name cannot exceed 200 characters.", exception.Message);
    }

    [Fact]
    public void Create_Line1TooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                CustomerId, "John Doe",
                new string('x', CustomerAddress.LineMaxLength + 1),
                "Cairo", "12345", "EG"));

        Assert.Equal("Address line cannot exceed 200 characters.", exception.Message);
    }

    [Fact]
    public void Create_PostalCodeTooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                CustomerId, "John Doe", "1 Main Street", "Cairo",
                new string('9', CustomerAddress.PostalCodeMaxLength + 1), "EG"));

        Assert.Equal("Postal code cannot exceed 20 characters.", exception.Message);
    }

    [Fact]
    public void Create_OptionalLine2TooLong_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            CustomerAddress.Create(
                CustomerId, "John Doe", "1 Main Street", "Cairo", "12345", "EG",
                line2: new string('x', CustomerAddress.LineMaxLength + 1)));

        Assert.Equal("Address line 2 cannot exceed 200 characters.", exception.Message);
    }

    // ---------- Update ----------

    [Fact]
    public void Update_ValidData_ReplacesAllFieldsAndTouches()
    {
        var address = NewAddress();
        var updatedAt = address.UpdatedAtUtc;

        address.Update(
            "Jane Doe", "2 Second Street", "Alexandria", "23456", "EG",
            line2: "Floor 3", state: "AL", phoneNumber: "01111111111");

        Assert.Equal("Jane Doe", address.FullName);
        Assert.Equal("2 Second Street", address.Line1);
        Assert.Equal("Alexandria", address.City);
        Assert.Equal("23456", address.PostalCode);
        Assert.Equal("EG", address.Country);
        Assert.Equal("Floor 3", address.Line2);
        Assert.Equal("AL", address.State);
        Assert.Equal("01111111111", address.PhoneNumber);
        Assert.True(address.UpdatedAtUtc >= updatedAt);
    }

    [Fact]
    public void Update_TrimsFields()
    {
        var address = NewAddress();

        address.Update("  Jane Doe  ", " 2 Second Street ", " Alexandria ", " 23456 ", " EG ");

        Assert.Equal("Jane Doe", address.FullName);
        Assert.Equal("2 Second Street", address.Line1);
        Assert.Equal("Alexandria", address.City);
        Assert.Equal("23456", address.PostalCode);
        Assert.Equal("EG", address.Country);
    }

    [Fact]
    public void Update_KeepsIsDefault()
    {
        var address = NewAddress(isDefault: true);

        address.Update("Jane Doe", "2 Second Street", "Alexandria", "23456", "EG");

        Assert.True(address.IsDefault); // editing never silently changes the flag
    }

    [Fact]
    public void Update_OptionalWhitespace_ClearsOptionalFields()
    {
        var address = NewAddress();

        address.Update(
            "John Doe", "1 Main Street", "Cairo", "12345", "EG",
            line2: "  ", state: null, phoneNumber: "");

        Assert.Null(address.Line2);
        Assert.Null(address.State);
        Assert.Null(address.PhoneNumber);
    }

    [Fact]
    public void Update_MissingRequiredField_ThrowsAndKeepsOriginalValues()
    {
        var address = NewAddress();

        // validate-then-assign: a rejected update must not half-change the entity
        Assert.Throws<DomainException>(() =>
            address.Update("Jane Doe", "2 Second Street", "", "23456", "EG"));

        Assert.Equal("John Doe", address.FullName);
        Assert.Equal("1 Main Street", address.Line1);
        Assert.Equal("Cairo", address.City);
        Assert.Equal("12345", address.PostalCode);
        Assert.Equal("EG", address.Country);
    }

    // ---------- Default flag ----------

    [Fact]
    public void SetDefault_WhenNotDefault_SetsFlag()
    {
        var address = NewAddress();

        address.SetDefault();

        Assert.True(address.IsDefault);
    }

    [Fact]
    public void SetDefault_WhenAlreadyDefault_IsNoOp()
    {
        var address = NewAddress(isDefault: true);
        var updatedAt = address.UpdatedAtUtc;

        address.SetDefault();

        Assert.True(address.IsDefault);
        Assert.Equal(updatedAt, address.UpdatedAtUtc); // no change -> no touch
    }

    [Fact]
    public void ClearDefault_WhenDefault_ClearsFlag()
    {
        var address = NewAddress(isDefault: true);

        address.ClearDefault();

        Assert.False(address.IsDefault);
    }

    [Fact]
    public void ClearDefault_WhenNotDefault_IsNoOp()
    {
        var address = NewAddress();
        var updatedAt = address.UpdatedAtUtc;

        address.ClearDefault();

        Assert.False(address.IsDefault);
        Assert.Equal(updatedAt, address.UpdatedAtUtc);
    }
}
