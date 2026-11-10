using CommerceFlow.Domain.Entities.Orders;
using CommerceFlow.Domain.Exceptions;

namespace CommerceFlow.Domain.Tests.Orders;

public class AddressSnapshotTests
{
    private static AddressSnapshot NewAddress() =>
        AddressSnapshot.Create(" John Doe ", " 123 Main St ", " Alexandria ", " 21500 ", " Egypt ");

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_TrimsRequiredFieldsAndLeavesOptionalsNull()
    {
        var address = NewAddress();

        Assert.Equal("John Doe", address.FullName);
        Assert.Equal("123 Main St", address.Line1);
        Assert.Equal("Alexandria", address.City);
        Assert.Equal("21500", address.PostalCode);
        Assert.Equal("Egypt", address.Country);
        Assert.Null(address.Line2);
        Assert.Null(address.State);
        Assert.Null(address.PhoneNumber);
    }

    [Fact]
    public void Create_AllOptionalsProvided_StoresTrimmedValues()
    {
        var address = AddressSnapshot.Create(
            "John Doe",
            "123 Main St",
            "Alexandria",
            "21500",
            "Egypt",
            line2: " Apt 4B ",
            state: " Alexandria Governorate ",
            phoneNumber: " 01001234567 ");

        Assert.Equal("Apt 4B", address.Line2);
        Assert.Equal("Alexandria Governorate", address.State);
        Assert.Equal("01001234567", address.PhoneNumber);
    }

    [Fact]
    public void Create_BlankOptionals_AreStoredAsNull()
    {
        var address = AddressSnapshot.Create(
            "John Doe", "123 Main", "Alex", "21500", "Egypt",
            line2: "   ",
            state: "   ",
            phoneNumber: "   ");

        Assert.Null(address.Line2);
        Assert.Null(address.State);
        Assert.Null(address.PhoneNumber);
    }

    // ---------- Required fields ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyFullName_Throws(string fullName)
    {
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create(fullName, "123 Main", "Alex", "21500", "Egypt"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyLine1_Throws(string line1)
    {
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", line1, "Alex", "21500", "Egypt"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyCity_Throws(string city)
    {
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", "123 Main", city, "21500", "Egypt"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyPostalCode_Throws(string postalCode)
    {
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", "123 Main", "Alex", postalCode, "Egypt"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyCountry_Throws(string country)
    {
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", "123 Main", "Alex", "21500", country));
    }

    [Fact]
    public void Create_NullFullName_Throws()
    {
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create(null!, "123 Main", "Alex", "21500", "Egypt"));
    }

    // ---------- Max lengths ----------

    [Fact]
    public void Create_FullNameLongerThanMaximum_Throws()
    {
        var tooLong = new string('a', AddressSnapshot.FullNameMaxLength + 1);
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create(tooLong, "123 Main", "Alex", "21500", "Egypt"));
    }

    [Fact]
    public void Create_Line1LongerThanMaximum_Throws()
    {
        var tooLong = new string('a', AddressSnapshot.LineMaxLength + 1);
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", tooLong, "Alex", "21500", "Egypt"));
    }

    [Fact]
    public void Create_CityLongerThanMaximum_Throws()
    {
        var tooLong = new string('a', AddressSnapshot.CityMaxLength + 1);
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", "123 Main", tooLong, "21500", "Egypt"));
    }

    [Fact]
    public void Create_PostalCodeLongerThanMaximum_Throws()
    {
        var tooLong = new string('1', AddressSnapshot.PostalCodeMaxLength + 1);
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", "123 Main", "Alex", tooLong, "Egypt"));
    }

    [Fact]
    public void Create_CountryLongerThanMaximum_Throws()
    {
        var tooLong = new string('a', AddressSnapshot.CountryMaxLength + 1);
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", "123 Main", "Alex", "21500", tooLong));
    }

    [Fact]
    public void Create_Line2LongerThanMaximum_Throws()
    {
        var tooLong = new string('a', AddressSnapshot.LineMaxLength + 1);
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", "123 Main", "Alex", "21500", "Egypt", line2: tooLong));
    }

    [Fact]
    public void Create_StateLongerThanMaximum_Throws()
    {
        var tooLong = new string('a', AddressSnapshot.StateMaxLength + 1);
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", "123 Main", "Alex", "21500", "Egypt", state: tooLong));
    }

    [Fact]
    public void Create_PhoneNumberLongerThanMaximum_Throws()
    {
        var tooLong = new string('9', AddressSnapshot.PhoneNumberMaxLength + 1);
        Assert.Throws<DomainException>(() =>
            AddressSnapshot.Create("John Doe", "123 Main", "Alex", "21500", "Egypt", phoneNumber: tooLong));
    }
}
