using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace ECommerce.Api.Authorization;

public sealed class SellerAuthorizationResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler
        _fallback = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext httpContext,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var hideAsNotFound =
            authorizeResult.Forbidden &&
            httpContext.Items.ContainsKey(
                SellerAuthorizationItems.ReturnNotFound);

        if (hideAsNotFound)
        {
            httpContext.Response.StatusCode =
                StatusCodes.Status404NotFound;

            return Task.CompletedTask;
        }

        return _fallback.HandleAsync(
            next,
            httpContext,
            policy,
            authorizeResult);
    }
}