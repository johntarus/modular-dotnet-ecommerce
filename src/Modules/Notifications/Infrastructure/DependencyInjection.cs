using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Features.Abstractions.Email;
using Notifications.Features.EventHandlers;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure;

internal static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Host),
                "Email SMTP host is required.")
            .Validate(
                options => options.Port > 0,
                "Email SMTP port must be valid.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SenderEmail),
                "Email sender address is required.")
            .ValidateOnStart();

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // FakeEmailService only logs the email. Switch to SmtpEmailSender once Email:* points at a real SMTP server
        // (appsettings currently has the smtp.example.com placeholder, so real sends would fail and be retried).
        services.AddScoped<IEmailSender, FakeEmailSender>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<CustomerRegisteredConsumer>();
            // Add Outbox for the Inbox pattern (Idempotency)
            x.AddEntityFrameworkOutbox<NotificationsDbContext>(o =>
            {
                o.UsePostgres();
            });
        });

        return services;
    }
}
