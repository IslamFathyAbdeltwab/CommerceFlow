using CommerceFlow.Domain.Entities.Addresses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Addresses;

internal sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.CustomerId).IsRequired();

        // Many addresses per customer...
        builder.HasIndex(a => a.CustomerId);

        // ...but at most one default: CreateAddress/SetDefaultAddress clear the
        // previous default before saving; this filtered index closes the race
        // between two concurrent sets (same belt-and-braces as Cart's active cart).
        builder.HasIndex(a => new { a.CustomerId, a.IsDefault })
            .IsUnique()
            .HasFilter("[IsDefault] = 1");

        builder.Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(CustomerAddress.FullNameMaxLength);
        builder.Property(a => a.Line1)
            .IsRequired()
            .HasMaxLength(CustomerAddress.LineMaxLength);
        builder.Property(a => a.Line2)
            .HasMaxLength(CustomerAddress.LineMaxLength);
        builder.Property(a => a.City)
            .IsRequired()
            .HasMaxLength(CustomerAddress.CityMaxLength);
        builder.Property(a => a.State)
            .HasMaxLength(CustomerAddress.StateMaxLength);
        builder.Property(a => a.PostalCode)
            .IsRequired()
            .HasMaxLength(CustomerAddress.PostalCodeMaxLength);
        builder.Property(a => a.Country)
            .IsRequired()
            .HasMaxLength(CustomerAddress.CountryMaxLength);
        builder.Property(a => a.PhoneNumber)
            .HasMaxLength(CustomerAddress.PhoneNumberMaxLength);

        builder.Property(a => a.IsDefault).IsRequired();
    }
}
