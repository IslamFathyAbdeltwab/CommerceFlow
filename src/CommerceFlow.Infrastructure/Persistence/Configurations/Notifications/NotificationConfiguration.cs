using CommerceFlow.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientId).IsRequired();
        builder.Property(n => n.Channel).IsRequired();

        builder.Property(n => n.Subject)
            .IsRequired()
            .HasMaxLength(Notification.SubjectMaxLength);
        builder.Property(n => n.Body)
            .IsRequired()
            .HasMaxLength(Notification.BodyMaxLength);
        builder.Property(n => n.RecipientAddress)
            .HasMaxLength(Notification.AddressMaxLength);
        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(Notification.RelatedEntityTypeMaxLength);

        builder.Property(n => n.Status).IsRequired();
        builder.Property(n => n.FailureReason)
            .HasMaxLength(Notification.FailureReasonMaxLength);
        builder.Property(n => n.FailureCount).IsRequired();

        // lookup indexes: the personal feed reads by recipient (+status), the
        // admin's "what is stuck" view scans status alone
        builder.HasIndex(n => new { n.RecipientId, n.Status });
        builder.HasIndex(n => n.Status);
    }
}
