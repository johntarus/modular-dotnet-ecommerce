using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Configurations;

public sealed class CustomerNotificationPreferenceConfiguration : IEntityTypeConfiguration<CustomerNotificationPreference>
{
    public void Configure(EntityTypeBuilder<CustomerNotificationPreference> builder)
    {
        builder.ToTable("CustomerNotificationPreferences");

        // The Keycloak Sub is the Primary Key
        builder.HasKey(x => x.CustomerId);

        builder.Property(x => x.CustomerId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ReceiveMarketingEmails)
            .IsRequired();

        builder.Property(x => x.ReceiveOrderUpdates)
            .IsRequired();
    }
}