using CommerceFlow.Domain.Entities.Returns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Returns;

internal sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("ReturnRequests");

        builder.HasKey(r => r.Id);

        // ID-only references (no navigations), same as Payment/Order/Shipment.
        builder.Property(r => r.OrderId).IsRequired();
        builder.Property(r => r.CustomerId).IsRequired();

        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.Reason).IsRequired();
        builder.Property(r => r.Notes)
            .HasMaxLength(ReturnRequest.NotesMaxLength);
        builder.Property(r => r.RejectionReason)
            .HasMaxLength(ReturnRequest.RejectionReasonMaxLength);

        // Lookup indexes for the list endpoint's filters.
        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => r.CustomerId);
        builder.HasIndex(r => r.Status);

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
{
    public void Configure(EntityTypeBuilder<ReturnItem> builder)
    {
        builder.ToTable("ReturnItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ReturnRequestId).IsRequired();
        builder.Property(i => i.ProductVariantId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        // one line per variant per request (the domain rejects duplicates)
        builder.HasIndex(i => new { i.ReturnRequestId, i.ProductVariantId }).IsUnique();
        builder.HasIndex(i => i.ProductVariantId);
    }
}
