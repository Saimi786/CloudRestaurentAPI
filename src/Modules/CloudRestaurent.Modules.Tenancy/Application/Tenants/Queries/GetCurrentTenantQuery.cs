using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Tenants.Queries;

public sealed record GetCurrentTenantQuery : IRequest<TenantDto>;

public sealed class GetCurrentTenantHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<GetCurrentTenantQuery, TenantDto>
{
    public async Task<TenantDto> Handle(GetCurrentTenantQuery request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var tenant = await db.Set<Domain.Tenants.Tenant>()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantDto(t.Id, t.Name, t.Slug, t.BusinessType, t.Plan, t.IsActive, t.LogoUrl))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Tenant", tenantId);

        return tenant;
    }
}
