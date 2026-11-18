using System.ComponentModel.DataAnnotations;

namespace Customers.Domain.Entities;

public sealed class CustomerAddress
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string? Label { get; private set; }
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string? RecipientName { get; private set; }
    public string CountryCode { get; private set; } = null!;
    public string? PostalCode { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public bool IsDefaultShipping { get; private set; }
    public bool IsDefaultBilling { get; private set; }

    private CustomerAddress() { }

    internal static CustomerAddress Create(
        Guid customerId,
        string recipientName,
        string? phoneNumber,
        string? email,
        string street,
        string city,
        string state,
        string? postalCode,
        string countryCode,
        string? label = null)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new ValidationException("Recipient name is required.");
        if (string.IsNullOrWhiteSpace(street))
            throw new ValidationException("Street is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new ValidationException("City is required.");
        if (string.IsNullOrWhiteSpace(state))
            throw new ValidationException("State is required.");
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
            throw new ValidationException("A valid two-character country code is required.");

        return new CustomerAddress
        {
            Id = Guid.NewGuid(),
            CustomerId =  customerId,
            RecipientName = recipientName.Trim(),
            PhoneNumber = NormalizeOptional(phoneNumber),
            Email = NormalizeOptional(email),
            Street = street.Trim(),
            City = city.Trim(),
            State = state.Trim(),
            PostalCode = NormalizeOptional(postalCode),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            Label = NormalizeOptional(label)
        };
    }

    internal void Update(
        string recipientName,
        string? phoneNumber,
        string? email,
        string street,
        string city,
        string state,
        string? postalCode,
        string countryCode,
        string? label = null)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new ValidationException(
                "Recipient name is required.");

        if (string.IsNullOrWhiteSpace(street))
            throw new ValidationException(
                "Street is required.");

        if (string.IsNullOrWhiteSpace(city))
            throw new ValidationException(
                "City is required.");

        if (string.IsNullOrWhiteSpace(state))
            throw new ValidationException(
                "State is required.");

        if (string.IsNullOrWhiteSpace(countryCode) ||
            countryCode.Trim().Length != 2)
        {
            throw new ValidationException(
                "A valid two-character country code is required.");
        }
        RecipientName = recipientName.Trim();
        PhoneNumber = NormalizeOptional(phoneNumber);
        Email = NormalizeOptional(email);
        Street = street.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = NormalizeOptional(postalCode);
        CountryCode = countryCode.Trim().ToUpperInvariant();
        Label = NormalizeOptional(label);
    }

    internal void SetDefaultShipping(bool value) => IsDefaultShipping = value;
    internal void SetDefaultBilling(bool value) => IsDefaultBilling = value;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}