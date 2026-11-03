using MediatR;

namespace Cart.Features.Carts.MergeGuestCart;

public record MergeGuestCartCommand : IRequest<MergeResult>;