namespace Notifications.Features.Abstractions.Email.Templates.WelcomeEmail;

public sealed class WelcomeEmailTemplate : IEmailTemplate<WelcomeEmailModel>
{
    public EmailMessage Generate(WelcomeEmailModel model)
    {
        // Sanitize user input to prevent XSS in the email client
        var safeName = System.Net.WebUtility.HtmlEncode(model.FirstName);

        // C# 11 Raw String Literal (notice the 3 quotes)
        var html = $$"""
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <style>
                            body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; line-height: 1.6; }
                            .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                            .btn { display: inline-block; padding: 10px 20px; background-color: #007bff; color: #ffffff; text-decoration: none; border-radius: 5px; margin-top: 20px; }
                        </style>
                    </head>
                    <body>
                        <div class="container">
                            <h1>Welcome, {safeName}!</h1>
                            <p>Thank you for creating an account with us. We are thrilled to have you on board.</p>
                            <p>Click the button below to start exploring our store.</p>
                            <a href="https://yourstore.com" class="btn">Start Shopping</a>
                        </div>
                    </body>
                    </html>
                    """;

        var plainText = $"Welcome, {model.FirstName}! Thank you for creating an account with us.";

        return new EmailMessage(
            To: model.Email,
            Subject: "Welcome to our store!",
            HtmlBody: html,
            PlainTextBody: plainText
        );
    }
}