using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Modules.SaaS.Application;
using CloudRestaurent.Modules.SaaS.Domain;
using CloudRestaurent.Modules.Tenancy.Application.Platform;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/platform")]
public sealed class PlatformController(IMediator mediator) : ControllerBase
{
    // ============== Tenants (SuperAdmin console) ==============

    [HttpGet("tenants")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<ActionResult<IReadOnlyList<PlatformTenantListItem>>> ListTenants(
        [FromQuery] bool includeInactive = true, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetPlatformTenantsQuery(includeInactive), ct));

    [HttpGet("tenants/{id:guid}")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<ActionResult<PlatformTenantDetails>> GetTenant(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetPlatformTenantQuery(id), ct));

    [HttpPost("tenants")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<ActionResult<PlatformTenantDetails>> CreateTenant(
        [FromBody] CreatePlatformTenantCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetTenant), new { id = dto.Id }, dto);
    }

    public sealed record UpdateTenantBody(string Name, int Plan);

    [HttpPut("tenants/{id:guid}")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<ActionResult<PlatformTenantDetails>> UpdateTenant(
        Guid id, [FromBody] UpdateTenantBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new UpdatePlatformTenantCommand(id, body.Name, body.Plan), ct));

    [HttpPost("tenants/{id:guid}/deactivate")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<IActionResult> DeactivateTenant(Guid id, CancellationToken ct)
    {
        await mediator.Send(new SetTenantActiveCommand(id, false), ct);
        return NoContent();
    }

    [HttpPost("tenants/{id:guid}/activate")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<IActionResult> ActivateTenant(Guid id, CancellationToken ct)
    {
        await mediator.Send(new SetTenantActiveCommand(id, true), ct);
        return NoContent();
    }

    /// <summary>
    /// All branches under a tenant — used by the SuperAdmin "Manage Business" page
    /// to drill into a business's locations.
    /// </summary>
    [HttpGet("tenants/{id:guid}/branches")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<ActionResult<IReadOnlyList<PlatformBranchDto>>> ListTenantBranches(
        Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetTenantBranchesQuery(id), ct));

    /// <summary>
    /// Flat list of every active branch across every tenant — used by the
    /// top-bar location picker for SuperAdmin to switch operating context.
    /// </summary>
    [HttpGet("branches")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<ActionResult<IReadOnlyList<PlatformBranchPickDto>>> ListAllBranches(
        CancellationToken ct) =>
        Ok(await mediator.Send(new GetAllPlatformBranchesQuery(), ct));

    /// <summary>
    /// Full branch + parent tenant + owner info — used by the SuperAdmin
    /// "Manage Location" page.
    /// </summary>
    [HttpGet("branches/{id:guid}")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<ActionResult<PlatformBranchDetailDto>> GetBranchDetail(
        Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetPlatformBranchQuery(id), ct));

    /// <summary>Users assigned to a specific branch (cross-tenant for SuperAdmin).</summary>
    [HttpGet("branches/{id:guid}/users")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<ActionResult<IReadOnlyList<UserSummary>>> ListBranchUsers(
        Guid id,
        [FromServices] IIdentityService identity,
        CancellationToken ct) =>
        Ok(await identity.ListUsersByBranchAsync(id, ct));

    public sealed record ResetUserPasswordBody(string NewPassword);

    /// <summary>
    /// SuperAdmin-only cross-tenant password reset. The normal /users/{id}/reset-password
    /// is tenant-scoped via the caller's JWT — this one bypasses that so the platform
    /// operator can rescue a locked-out admin in any tenant.
    /// </summary>
    [HttpPost("users/{id:guid}/reset-password")]
    [HasPermission(AppPermissions.PlatformManageTenants)]
    public async Task<IActionResult> ResetUserPassword(
        Guid id,
        [FromBody] ResetUserPasswordBody body,
        [FromServices] IIdentityService identity,
        CancellationToken ct)
    {
        await identity.ResetPasswordCrossTenantAsync(id, body.NewPassword, ct);
        return NoContent();
    }

    // ============== Packages ==============

    [HttpGet("packages")]
    [HasPermission(AppPermissions.PlatformManagePackages)]
    public async Task<ActionResult<IReadOnlyList<PackageDto>>> Packages(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetPackagesQuery(includeInactive), ct));

    [HttpPost("packages")]
    [HasPermission(AppPermissions.PlatformManagePackages)]
    public async Task<ActionResult<PackageDto>> CreatePackage(
        [FromBody] CreatePackageCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpGet("subscriptions")]
    [HasPermission(AppPermissions.PlatformApproveSubscriptions)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionDto>>> Subscriptions(
        [FromQuery] SubscriptionStatus? status, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetSubscriptionsQuery(status), ct));

    public sealed record RequestSubscriptionBody(Guid TenantId, Guid PackageId, string? CouponCode, string? Notes);

    [HttpPost("subscriptions/request")]
    public async Task<ActionResult<SubscriptionDto>> RequestSubscription(
        [FromBody] RequestSubscriptionBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new RequestSubscriptionCommand(
            body.TenantId, body.PackageId, body.CouponCode, body.Notes), ct));

    public sealed record ApproveBody(DateOnly StartDate, DateOnly NextBillingDate);

    [HttpPost("subscriptions/{id:guid}/approve")]
    [HasPermission(AppPermissions.PlatformApproveSubscriptions)]
    public async Task<ActionResult<SubscriptionDto>> Approve(
        Guid id, [FromBody] ApproveBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new ApproveSubscriptionCommand(id, body.StartDate, body.NextBillingDate), ct));

    public sealed record CancelBody(DateOnly EndDate, string? Reason);

    [HttpPost("subscriptions/{id:guid}/cancel")]
    [HasPermission(AppPermissions.PlatformApproveSubscriptions)]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelBody body, CancellationToken ct)
    {
        await mediator.Send(new CancelSubscriptionCommand(id, body.EndDate, body.Reason), ct);
        return NoContent();
    }
}
