using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Features.UpdateNotificationPreferences;

internal sealed class UpdateNotificationPreferencesHandler(
    NotificationsDbContext context, 
    ICurrentUser currentUser) 
    : IRequestHandler<UpdateNotificationPreferencesCommand>
{
    public async Task Handle(UpdateNotificationPreferencesCommand request, CancellationToken ct)
    {
        var customerId = currentUser.UserId!.Value.ToString();

        var prefs = await context.CustomerNotificationPreferences
            .FirstOrDefaultAsync(p => p.CustomerId == customerId, ct);

        if (prefs is null)
        {
            prefs = CustomerNotificationPreference.CreateDefault(customerId);
            context.CustomerNotificationPreferences.Add(prefs);
        }

        prefs.UpdatePreferences(request.Marketing, request.OrderUpdates);
        await context.SaveChangesAsync(ct);
    }
}