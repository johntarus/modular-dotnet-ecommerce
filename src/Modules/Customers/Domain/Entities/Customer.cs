using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Domain;
using Customers.Domain.Events;

namespace Customers.Domain.Entities;

public sealed class Customer : BaseEntity
{
    private readonly List<CustomerAddress> _addresses = [];

    public Guid Id { get; private set; }
    public string IdentitySubject { get; private set; } = null!; // Keycloak Sub
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();
    
    // Quick access to default addresses
    public CustomerAddress? DefaultShippingAddress => _addresses.FirstOrDefault(a => a.IsDefaultShipping);
    public CustomerAddress? DefaultBillingAddress => _addresses.FirstOrDefault(a => a.IsDefaultBilling);
    
    private Customer() { }
    
    public static Customer Create(
        string identitySubject,
        string? email = null,
        string? firstName = null,
        string? lastName = null)
    {
        if (string.IsNullOrWhiteSpace(identitySubject))
            throw new ValidationException("Identity subject is required.");

        var customer =  new Customer
        {
            Id = Guid.NewGuid(),
            IdentitySubject = identitySubject,
            Email = NormalizeOptional(email),
            FirstName = NormalizeOptional(firstName),
            LastName = NormalizeOptional(lastName),
            IsActive = true, // Default to true
            CreatedAt = DateTime.UtcNow
        };
        customer.AddDomainEvent(new CustomerRegisteredDomainEvent(
            customer.Id,
            customer.Email,
            customer.FirstName
        ));
        return customer;
    }
    
    public void UpdateProfile(
        string? firstName,
        string? lastName,
        string? email,
        string? phoneNumber)
    {
        FirstName = NormalizeOptional(firstName);
        LastName = NormalizeOptional(lastName);
        Email = NormalizeOptional(email);
        PhoneNumber = NormalizeOptional(phoneNumber);

        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Deactivate()
    {
        if (!IsActive) return; // Already deactivated
    
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive) return;
    
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid AddAddress(
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
        var address = CustomerAddress.Create(customerId,
            recipientName, phoneNumber, email, 
            street, city, state, postalCode, countryCode, label);

        _addresses.Add(address);
        UpdatedAt = DateTime.UtcNow;

        return address.Id;
    }
    
    public void UpdateAddress(
        Guid addressId,
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
        var address = GetAddress(addressId);

        address.Update(
            recipientName, phoneNumber, email, 
            street, city, state, postalCode, countryCode, label);

        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RemoveAddress(Guid addressId)
    {
        var address = GetAddress(addressId);

        _addresses.Remove(address);

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDefaultShippingAddress(Guid addressId)
    {
        var selectedAddress = GetAddress(addressId);

        foreach (var address in _addresses)
        {
            address.SetDefaultShipping(
                address.Id == selectedAddress.Id);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDefaultBillingAddress(Guid addressId)
    {
        var selectedAddress = GetAddress(addressId);

        foreach (var address in _addresses)
        {
            address.SetDefaultBilling(
                address.Id == selectedAddress.Id);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    private CustomerAddress GetAddress(Guid addressId)
    {
        return _addresses.FirstOrDefault(
                   x => x.Id == addressId)
               ?? throw new ValidationException(
                   "The specified address does not belong to this customer.");
    }
    
    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}