namespace Customers.Features.Customers.GetProfile
{
    public record ProfileResponse(
        Guid Id,
        string IdentitySubject,
        string? FirstName,
        string? LastName,
        string? Email,
        string? PhoneNumber);
}