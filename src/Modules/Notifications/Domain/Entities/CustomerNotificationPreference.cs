namespace Notifications.Domain.Entities;

public class CustomerNotificationPreference
{
    public string CustomerId { get; private set; } // Keycloak Sub
    public bool ReceiveMarketingEmails { get; private set; }
    public bool ReceiveOrderUpdates { get; private set; }

    private CustomerNotificationPreference() { }

    public static CustomerNotificationPreference CreateDefault(string customerId)
    {
        return new CustomerNotificationPreference
        {
            CustomerId = customerId,
            ReceiveMarketingEmails = true,
            ReceiveOrderUpdates = true
        };
    }

    public void UpdatePreferences(bool marketing, bool orderUpdates)
    {
        ReceiveMarketingEmails = marketing;
        ReceiveOrderUpdates = orderUpdates;
    }
}