namespace Cart.Contracts;

public interface ICartModuleApi
{
    Task<CartCheckoutDto?> GetCartForCheckoutAsync(
        Guid cartId, 
        string identitySubject, 
        CancellationToken ct = default);
}

public sealed record CartCheckoutDto(
    Guid CartId,
    List<CartItemDto> Items);

public sealed record CartItemDto(
    Guid ProductId,
    int Quantity);