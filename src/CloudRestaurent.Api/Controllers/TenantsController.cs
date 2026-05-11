using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Modules.Tenancy.Application.Tenants.Commands;
using CloudRestaurent.Modules.Tenancy.Application.Tenants.Dtos;
using CloudRestaurent.Modules.Tenancy.Application.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tenants")]
public sealed class TenantsController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<TenantDto>> GetCurrent(CancellationToken ct) =>
        Ok(await mediator.Send(new GetCurrentTenantQuery(), ct));

    /// <summary>
    /// Upload tenant logo. Stored under wwwroot/uploads/{tenantId}/logo.{ext}
    /// and served as a static file. We deliberately overwrite the same filename
    /// so the public URL is stable across re-uploads.
    /// </summary>
    [HttpPost("me/logo")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    [RequestSizeLimit(2_000_000)] // 2 MB cap; logos should be tiny
    public async Task<ActionResult<TenantDto>> UploadLogo(
        IFormFile file,
        [FromServices] IWebHostEnvironment env,
        [FromServices] ITenantContext tenantContext,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = ["A logo file is required."]
            });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp" or ".svg"))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = ["Only PNG, JPG, WEBP, or SVG files are allowed."]
            });

        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
            "uploads", tenantId.ToString());
        Directory.CreateDirectory(dir);
        var filename = $"logo{ext}";
        var fullPath = Path.Combine(dir, filename);

        // Overwrite. Don't delete previous extensions — they'll be orphans, fine for v1.
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var publicUrl = $"/uploads/{tenantId}/{filename}";
        return Ok(await mediator.Send(new UploadTenantLogoCommand(publicUrl), ct));
    }
}
