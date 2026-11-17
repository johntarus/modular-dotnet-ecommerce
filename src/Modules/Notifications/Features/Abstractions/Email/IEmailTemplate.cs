namespace Notifications.Features.Abstractions.Email;

public interface IEmailTemplate<in TModel>
{
    EmailMessage Generate(TModel model);
}