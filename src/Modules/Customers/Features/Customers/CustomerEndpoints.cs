using Customers.Features.Customers.ActivateProfile;
using Customers.Features.Customers.DeactivateProfile;
using Customers.Features.Customers.GetProfile;
using Customers.Features.Customers.UpdateProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Customers.Features.Customers;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/me")
            .WithTags("Profile");

        group.MapGet("/", GetProfile);
        group.MapPut("/", UpdateProfile);
        group.MapDelete("/deactivate", DeactivateProfile)
            .WithName("DeactivateCustomerProfile");
        group.MapPost("/activate", ActivateProfile)
            .WithName("ActivateCustomerProfile");
    }

    private static async Task<IResult> GetProfile(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetProfileQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileCommand command,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateProfile(
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new DeactivateCustomerCommand(), ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ActivateProfile(
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send(new ActivateCustomerCommand(), ct);

        return Results.Ok(new
        {
            Message = "Account successfully reactivated."
        });
    }
}