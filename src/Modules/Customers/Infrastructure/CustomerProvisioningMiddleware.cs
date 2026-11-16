using BuildingBlocks.Infrastructure.Auth.CurrentUser;
using Customers.Domain.Entities;
using Customers.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure;

public class CustomerProvisioningMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, CustomersDbContext dbContext, ICurrentUser currentUser)
    {
        if (currentUser.IsAuthenticated && currentUser.UserId.HasValue)
        {
            var keycloakId = currentUser.UserId.Value.ToString();
            
            var customer = await dbContext.Customers
                .FirstOrDefaultAsync(c => c.IdentitySubject == keycloakId, context.RequestAborted);

            if (customer is null)
            {
                var email = context.User.FindFirst("email")?.Value;
                var firstName = context.User.FindFirst("given_name")?.Value;
                var lastName = context.User.FindFirst("family_name")?.Value;

                customer = Customer.Create(keycloakId, email, firstName, lastName);
                dbContext.Customers.Add(customer);
                await dbContext.SaveChangesAsync(context.RequestAborted);
            }
            else if (!customer.IsActive)
            {
                //  BYPASS: Allow deactivated users to hit the activate endpoint
                if (context.Request.Path.StartsWithSegments("/api/customers/me/activate"))
                {
                    await next(context);
                    return;
                }

                //  BLOCK: Deny all other requests
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new 
                { 
                    Error = "Account Deactivated", 
                    Message = "Your account has been deactivated. Please reactivate it to continue." 
                });
                
                return; 
            }
        }

        await next(context);
    }
}