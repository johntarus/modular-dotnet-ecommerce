
using Customers.Features.Addresses.AddAddress;
using Customers.Features.Addresses.DeleteAddress;
using Customers.Features.Addresses.GetAddressById;
using Customers.Features.Addresses.GetAddresses;
using Customers.Features.Addresses.SetDefaultAddress;
using Customers.Features.Addresses.UpdateAddress;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Customers.Features.Addresses;

public static class AddressEndpoints
{
    public static RouteGroupBuilder MapAddressEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/addresses")
            .WithTags("Addresses");

        group.MapGet("/", GetAddresses);
        group.MapGet("/{addressId:guid}", GetAddressById);
        group.MapPost("/", AddAddress);
        group.MapPut("/{addressId:guid}", UpdateAddress);
        group.MapDelete("/{addressId:guid}", DeleteAddress);
        group.MapPut("/{addressId:guid}/default-shipping", SetDefaultShipping);
        group.MapPut("/{addressId:guid}/default-billing", SetDefaultBilling);

        return group;
    }

    private static async Task<IResult> GetAddresses(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetAddressesQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAddressById(
        Guid addressId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetAddressQuery(addressId), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> AddAddress(
        AddAddressCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Results.Created($"/api/customers/addresses/{id}", new { Id = id });
    }

    private static async Task<IResult> UpdateAddress(
        Guid addressId,
        UpdateAddressCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var updateCommand = command with { AddressId = addressId };

        await sender.Send(updateCommand, ct);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAddress(
        Guid addressId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeleteAddressCommand(addressId), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetDefaultShipping(
        Guid addressId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(
            new SetDefaultAddressCommand(addressId, AddressType.Shipping), ct);

        return Results.NoContent();
    }

    private static async Task<IResult> SetDefaultBilling(
        Guid addressId,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(
            new SetDefaultAddressCommand(addressId, AddressType.Billing), ct);

        return Results.NoContent();
    }
}
