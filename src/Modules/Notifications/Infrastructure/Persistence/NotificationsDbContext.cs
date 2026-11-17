using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Entities; // Make sure this is here

namespace Notifications.Infrastructure.Persistence;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    
    public DbSet<CustomerNotificationPreference> CustomerNotificationPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}