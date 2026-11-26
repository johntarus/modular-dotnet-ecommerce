using Newtonsoft.Json;

namespace Orders.Domain.ValueObjects;

public sealed record DeliveryAddress
{
    public string RecipientName { get; }
    public string PhoneNumber { get; }
    public string AddressLine { get; }
    public string City { get; }
    public string County { get; }
    public string Country { get; }
    public string PostalCode { get; }

    public DeliveryAddress(
        string recipientName,
        string phoneNumber,
        string addressLine,
        string city,
        string county,
        string country,
        string postalCode)
    {
        RecipientName = Required(recipientName, nameof(recipientName));
        PhoneNumber = Required(phoneNumber, nameof(phoneNumber));
        AddressLine = Required(addressLine, nameof(addressLine));
        City = Required(city, nameof(city));
        County = Required(county, nameof(county));
        Country = Required(country, nameof(country));
        PostalCode = Required(postalCode, nameof(postalCode));
    }

    public static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required", parameterName);
        return value.Trim();
    }
}