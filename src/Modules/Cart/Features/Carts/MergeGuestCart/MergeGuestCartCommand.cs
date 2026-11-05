using MediatR;

namespace Cart.Features.Carts.MergeGuestCart;

public record MergeGuestCartCommand(string? guestToken) : IRequest<MergeResult>;