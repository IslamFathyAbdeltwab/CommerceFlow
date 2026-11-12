using CommerceFlow.Domain.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Payments;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        // ID-only references (no navigations), same as Order/Address.
        builder.Property(p => p.OrderId).IsRequired();
        builder.Property(p => p.CustomerId).IsRequired();

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(Payment.CurrencyLength);
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.Provider)
            .IsRequired()
            .HasMaxLength(Payment.ProviderMaxLength);
        builder.Property(p => p.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(Payment.IdempotencyKeyMaxLength);
        builder.Property(p => p.ProviderTransactionId)
            .HasMaxLength(Payment.ProviderTransactionIdMaxLength);
        builder.Property(p => p.FailureReason)
            .HasMaxLength(Payment.FailureReasonMaxLength);

        // Lookup indexes for the list endpoint's filters.
        builder.HasIndex(p => p.OrderId);
        builder.HasIndex(p => p.CustomerId);

        // The idempotency contract, enforced by the database: two retried
        // creates racing for the same (order, key) cannot both insert - one
        // wins, the loser's unique violation is the "replay" signal.
        builder.HasIndex(p => new { p.OrderId, p.IdempotencyKey }).IsUnique();
    }
}
