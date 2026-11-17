using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Configurations;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Recipient)
            .IsRequired()
            .HasMaxLength(255); // Emails or phone numbers won't exceed this

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Body)
            .IsRequired(); // No max length, allows for unlimited HTML text
        
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000); // Prevent massive stack traces from blowing up the column

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.SentAt)
            .IsRequired(false);

        // Indexes for Performance
        // Admin usually sorts by date, and searches by recipient
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Recipient);
    }
}