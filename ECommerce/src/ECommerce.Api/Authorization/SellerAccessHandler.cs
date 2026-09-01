using ECommerce.Application.Abstractions.Authentication;
using ECommerce.Application.Abstractions.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.Api.Authorization;

public sealed class SellerAccessHandler(
    ICurrentUser currentUser,
    ISellerAccessReader accessReader)
    : AuthorizationHandler<SellerAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SellerAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            throw new InvalidOperationException(
                "Seller authorization requires an HTTP context.");
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = currentUser.UserId;

        if (!userId.HasValue ||
            userId.Value == Guid.Empty)
        {
            context.Fail();
            return;
        }

        if (!httpContext.Request.RouteValues.TryGetValue(
                "sellerId",
                out var sellerIdValue))
        {
            throw new InvalidOperationException(
                "Seller authorization was applied to a route " +
                "without a sellerId parameter.");
        }

        if (!Guid.TryParse(
                sellerIdValue?.ToString(),
                out var sellerId) ||
            sellerId == Guid.Empty)
        {
            MarkAsNotFound(httpContext);
            context.Fail();
            return;
        }

        var access =
            await accessReader.FindActiveMembershipAsync(
                userId.Value,
                sellerId,
                httpContext.RequestAborted);

        if (access is null)
        {
            MarkAsNotFound(httpContext);
            context.Fail();
            return;
        }

        var activeRoles =
            access.ActiveNormalizedRoles.ToHashSet(
                StringComparer.Ordinal);

        if (activeRoles.Count == 0)
        {
            context.Fail();
            return;
        }

        if (requirement.RequiredNormalizedRoles.Count == 0 ||
            requirement.RequiredNormalizedRoles.Any(
                activeRoles.Contains))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }

    private static void MarkAsNotFound(
        HttpContext httpContext)
    {
        httpContext.Items[
            SellerAuthorizationItems.ReturnNotFound] = true;
    }
}

internal static class SellerAuthorizationItems
{
    internal static readonly object ReturnNotFound = new();
}