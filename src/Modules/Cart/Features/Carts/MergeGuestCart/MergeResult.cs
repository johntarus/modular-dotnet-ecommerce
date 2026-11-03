namespace Cart.Features.Carts.MergeGuestCart;

public record MergeResult(
    Guid CartId,
    int MergedItemCount,
    List<string> Warnings);