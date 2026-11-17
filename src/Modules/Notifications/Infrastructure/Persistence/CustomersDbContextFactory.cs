using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Notifications.Infrastructure.Persistence;

// Used only by `dotnet ef` (migrations) at design time.
internal sealed class NotificationsDbContextFactory
    : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var optionBuilder = new DbContextOptionsBuilder<NotificationsDbContext>();
        optionBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=modular_monolith;Username=postgres;Password=postgres");

        return new NotificationsDbContext(optionBuilder.Options);
    }
}
