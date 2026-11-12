using CommerceFlow.Application.Addresses.Commands.CreateAddress;
using CommerceFlow.Application.Addresses.Commands.DeleteAddress;
using CommerceFlow.Application.Addresses.Commands.SetDefaultAddress;
using CommerceFlow.Application.Addresses.Commands.UpdateAddress;
using CommerceFlow.Application.Addresses.Queries.GetAddresses;
using CommerceFlow.Domain.Entities.Addresses;

namespace CommerceFlow.Application.Tests.Addresses;

public sealed class AddressValidatorTests
{
    private static string TooLong(int length) => new('x', length);

    // ---------- CreateAddress ----------

    private static CreateAddressCommand ValidCreate() => AddressFixtures.ValidCreateCommand();

    [Fact]
    public void Create_WithValidInput_IsValid()
    {
        var result = new CreateAddressCommandValidator().Validate(ValidCreate());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithNullOptionals_IsValid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { Line2 = null, State = null, PhoneNumber = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_WithEmptyCustomerId_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { CustomerId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.CustomerId));
    }

    [Fact]
    public void Create_WithEmptyFullName_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { FullName = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.FullName));
    }

    [Fact]
    public void Create_WithFullNameTooLong_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { FullName = TooLong(CustomerAddress.FullNameMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.FullName));
    }

    [Fact]
    public void Create_WithEmptyLine1_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { Line1 = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.Line1));
    }

    [Fact]
    public void Create_WithLine1TooLong_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { Line1 = TooLong(CustomerAddress.LineMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.Line1));
    }

    [Fact]
    public void Create_WithLine2TooLong_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { Line2 = TooLong(CustomerAddress.LineMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.Line2));
    }

    [Fact]
    public void Create_WithEmptyCity_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { City = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.City));
    }

    [Fact]
    public void Create_WithCityTooLong_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { City = TooLong(CustomerAddress.CityMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.City));
    }

    [Fact]
    public void Create_WithStateTooLong_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { State = TooLong(CustomerAddress.StateMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.State));
    }

    [Fact]
    public void Create_WithEmptyPostalCode_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { PostalCode = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.PostalCode));
    }

    [Fact]
    public void Create_WithPostalCodeTooLong_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { PostalCode = TooLong(CustomerAddress.PostalCodeMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.PostalCode));
    }

    [Fact]
    public void Create_WithEmptyCountry_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { Country = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.Country));
    }

    [Fact]
    public void Create_WithCountryTooLong_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { Country = TooLong(CustomerAddress.CountryMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.Country));
    }

    [Fact]
    public void Create_WithPhoneNumberTooLong_IsInvalid()
    {
        var result = new CreateAddressCommandValidator()
            .Validate(ValidCreate() with { PhoneNumber = TooLong(CustomerAddress.PhoneNumberMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAddressCommand.PhoneNumber));
    }

    // ---------- UpdateAddress ----------

    private static UpdateAddressCommand ValidUpdate() =>
        AddressFixtures.ValidUpdateCommand(Guid.NewGuid());

    [Fact]
    public void Update_WithValidInput_IsValid()
    {
        var result = new UpdateAddressCommandValidator().Validate(ValidUpdate());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Update_WithEmptyAddressId_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { AddressId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.AddressId));
    }

    [Fact]
    public void Update_WithEmptyFullName_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { FullName = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.FullName));
    }

    [Fact]
    public void Update_WithFullNameTooLong_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { FullName = TooLong(CustomerAddress.FullNameMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.FullName));
    }

    [Fact]
    public void Update_WithEmptyLine1_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { Line1 = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.Line1));
    }

    [Fact]
    public void Update_WithLine1TooLong_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { Line1 = TooLong(CustomerAddress.LineMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.Line1));
    }

    [Fact]
    public void Update_WithLine2TooLong_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { Line2 = TooLong(CustomerAddress.LineMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.Line2));
    }

    [Fact]
    public void Update_WithEmptyCity_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { City = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.City));
    }

    [Fact]
    public void Update_WithCityTooLong_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { City = TooLong(CustomerAddress.CityMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.City));
    }

    [Fact]
    public void Update_WithStateTooLong_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { State = TooLong(CustomerAddress.StateMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.State));
    }

    [Fact]
    public void Update_WithEmptyPostalCode_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { PostalCode = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.PostalCode));
    }

    [Fact]
    public void Update_WithPostalCodeTooLong_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { PostalCode = TooLong(CustomerAddress.PostalCodeMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.PostalCode));
    }

    [Fact]
    public void Update_WithEmptyCountry_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { Country = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.Country));
    }

    [Fact]
    public void Update_WithCountryTooLong_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { Country = TooLong(CustomerAddress.CountryMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.Country));
    }

    [Fact]
    public void Update_WithPhoneNumberTooLong_IsInvalid()
    {
        var result = new UpdateAddressCommandValidator()
            .Validate(ValidUpdate() with { PhoneNumber = TooLong(CustomerAddress.PhoneNumberMaxLength + 1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAddressCommand.PhoneNumber));
    }

    // ---------- SetDefaultAddress ----------

    [Fact]
    public void SetDefault_WithValidInput_IsValid()
    {
        var result = new SetDefaultAddressCommandValidator()
            .Validate(new SetDefaultAddressCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SetDefault_WithEmptyAddressId_IsInvalid()
    {
        var result = new SetDefaultAddressCommandValidator()
            .Validate(new SetDefaultAddressCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(SetDefaultAddressCommand.AddressId));
    }

    // ---------- DeleteAddress ----------

    [Fact]
    public void Delete_WithValidInput_IsValid()
    {
        var result = new DeleteAddressCommandValidator()
            .Validate(new DeleteAddressCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Delete_WithEmptyAddressId_IsInvalid()
    {
        var result = new DeleteAddressCommandValidator()
            .Validate(new DeleteAddressCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteAddressCommand.AddressId));
    }

    // ---------- GetAddresses (query) ----------

    [Fact]
    public void GetAddresses_WithDefaults_IsValid()
    {
        var result = new GetAddressesQueryValidator().Validate(new GetAddressesQuery());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void GetAddresses_WithOutOfRangePageNumber_IsInvalid(int pageNumber)
    {
        var result = new GetAddressesQueryValidator()
            .Validate(new GetAddressesQuery(PageNumber: pageNumber));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAddressesQuery.PageNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void GetAddresses_WithOutOfRangePageSize_IsInvalid(int pageSize)
    {
        var result = new GetAddressesQueryValidator()
            .Validate(new GetAddressesQuery(PageSize: pageSize));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAddressesQuery.PageSize));
    }
}
