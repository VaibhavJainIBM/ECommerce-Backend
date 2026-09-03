using ECommerce.Api.Authorization;
using ECommerce.Application.Common;
using ECommerce.Application.SellerTeams;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Policy = SellerPolicies.Owner)]
[Route("api/sellers/{sellerId:guid}")]
public sealed class SellerTeamController(ISellerTeamService service) : ControllerBase
{
    [HttpGet("roles")]
    public IActionResult Roles() => Ok(new[]
    {
        new SellerRoleResponseDto(SellerRoleNames.Owner, "Manage the seller, team, listings, warehouses, inventory, and seller orders."),
        new SellerRoleResponseDto(SellerRoleNames.Manager, "Manage listings, warehouses, inventory, and seller orders; cannot manage members or roles."),
        new SellerRoleResponseDto(SellerRoleNames.WarehouseStaff, "Read assigned warehouses and manage inventory only within those warehouses.")
    });

    [HttpGet("members")]
    public async Task<IActionResult> Members(Guid sellerId, CancellationToken ct) => Respond(await service.GetMembersAsync(sellerId, ct));
    [HttpGet("members/{memberId:guid}", Name = "SellerMemberById")]
    public async Task<IActionResult> Member(Guid sellerId, Guid memberId, CancellationToken ct) => Respond(await service.GetMemberAsync(sellerId, memberId, ct));
    [HttpPost("members")]
    public async Task<IActionResult> Invite(Guid sellerId, [FromBody] InviteSellerMemberRequestDto? request, CancellationToken ct)
    {
        var result = await service.InviteAsync(sellerId, request, ct);
        return result.IsFailure ? Respond(result) :
            CreatedAtRoute("SellerMemberById", new { sellerId, memberId = result.Value!.MemberId }, result.Value);
    }
    [HttpPost("members/{memberId:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid sellerId, Guid memberId, CancellationToken ct) => Respond(await service.ChangeStateAsync(sellerId, memberId, "suspend", ct));
    [HttpPost("members/{memberId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid sellerId, Guid memberId, CancellationToken ct) => Respond(await service.ChangeStateAsync(sellerId, memberId, "reactivate", ct));
    [HttpDelete("members/{memberId:guid}")]
    public async Task<IActionResult> Remove(Guid sellerId, Guid memberId, CancellationToken ct) => Respond(await service.ChangeStateAsync(sellerId, memberId, "remove", ct));
    [HttpPut("members/{memberId:guid}/roles/{roleName}")]
    public async Task<IActionResult> AssignRole(Guid sellerId, Guid memberId, string roleName, CancellationToken ct) => Respond(await service.SetRoleAsync(sellerId, memberId, roleName, true, ct));
    [HttpDelete("members/{memberId:guid}/roles/{roleName}")]
    public async Task<IActionResult> RevokeRole(Guid sellerId, Guid memberId, string roleName, CancellationToken ct) => Respond(await service.SetRoleAsync(sellerId, memberId, roleName, false, ct));
    [HttpPut("members/{memberId:guid}/warehouses/{warehouseId:guid}")]
    public async Task<IActionResult> AssignWarehouse(Guid sellerId, Guid memberId, Guid warehouseId, CancellationToken ct) => Respond(await service.SetWarehouseAsync(sellerId, memberId, warehouseId, true, ct));
    [HttpDelete("members/{memberId:guid}/warehouses/{warehouseId:guid}")]
    public async Task<IActionResult> UnassignWarehouse(Guid sellerId, Guid memberId, Guid warehouseId, CancellationToken ct) => Respond(await service.SetWarehouseAsync(sellerId, memberId, warehouseId, false, ct));

    private IActionResult Respond<T>(Result<T> result) => SellerTeamResults.Respond(this, result);
}

[ApiController]
[Authorize]
public sealed class SellerInvitationsController(ISellerTeamService service) : ControllerBase
{
    [HttpGet("api/seller-invitations")]
    public async Task<IActionResult> Mine(CancellationToken ct) => SellerTeamResults.Respond(this, await service.GetInvitationsAsync(ct));
    [HttpPost("api/sellers/{sellerId:guid}/invitations/accept")]
    public async Task<IActionResult> Accept(Guid sellerId, CancellationToken ct) => SellerTeamResults.Respond(this, await service.AcceptAsync(sellerId, ct));
}

internal static class SellerTeamResults
{
    public static IActionResult Respond<T>(ControllerBase controller, Result<T> result)
    {
        if (result.IsSuccess) return controller.Ok(result.Value);
        var error = result.Errors.First();
        var status = error.Code switch
        {
            "seller_team.not_found" => 404,
            "seller_team.unauthorized" => 401,
            "seller_team.conflict" => 409,
            _ => 400
        };
        return controller.Problem(statusCode: status, title: "Seller team request failed.", detail: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}

