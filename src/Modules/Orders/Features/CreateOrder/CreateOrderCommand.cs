using MediatR;

namespace Orders.Features.CreateOrder;

public record CreateOrderCommand(
    Guid CartId,
    Guid AddressId
) : IRequest<Guid>;