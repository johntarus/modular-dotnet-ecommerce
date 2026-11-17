namespace Notifications.Features.Abstractions.Email.Templates.WelcomeEmail;

public sealed record WelcomeEmailModel(
    string Email,
    string FirstName
);