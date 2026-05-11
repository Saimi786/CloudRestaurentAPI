using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Tenants.Commands;

/// <summary>
/// Handler is invoked AFTER the controller has streamed the file to disk.
/// Controller passes the public URL it produced; we just persist it on the Tenant
/// row. Keeping disk I/O in the controller avoids leaking IFileSystem/IWebHostEnvironment
/// into Application layer (which has no Web dependency).
/// </summary>
public sealed record UploadTenantLogoCommand(string PublicUrl) : IRequest<TenantDto>;

public sealed class UploadTenantLogoHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<UploadTenantLogoCommand, TenantDto>
{
    public async Task<TenantDto> Handle(UploadTenantLogoCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var tenant = await db.Set<Domain.Tenants.Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        tenant.SetLogoUrl(request.PublicUrl);
        await db.SaveChangesAsync(ct);

        return new TenantDto(tenant.Id, tenant.Name, tenant.Slug,
            tenant.BusinessType, tenant.Plan, tenant.IsActive, tenant.LogoUrl);
    }
}
