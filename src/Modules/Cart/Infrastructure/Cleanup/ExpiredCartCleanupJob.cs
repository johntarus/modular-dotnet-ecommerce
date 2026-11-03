using Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class ExpiredCartCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredCartCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CartDbContext>();

                var deleted = await db.Carts
                    .Where(c => c.GuestTokenHash != null && c.ExpiresAt < DateTime.UtcNow)
                    .ExecuteDeleteAsync(ct);

                if (deleted > 0)
                    logger.LogInformation("Cleaned up {Count} expired guest carts.", deleted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Guest cart cleanup failed.");
            }
        }
    }
}