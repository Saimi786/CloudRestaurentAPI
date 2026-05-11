using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Pricing.Application.MixMatch.Dtos;
using CloudRestaurent.Modules.Pricing.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Pricing.Application.MixMatch.Queries;

public sealed record GetMixMatchGroupsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<MixMatchGroupDto>>;

public sealed class GetMixMatchGroupsHandler(IAppDbContext db)
    : IRequestHandler<GetMixMatchGroupsQuery, IReadOnlyList<MixMatchGroupDto>>
{
    public async Task<IReadOnlyList<MixMatchGroupDto>> Handle(GetMixMatchGroupsQuery request, CancellationToken ct)
    {
        var query = db.Set<MixMatchGroup>().AsNoTracking();
        if (!request.IncludeInactive) query = query.Where(g => g.IsActive);

        var groups = await query
            .OrderByDescending(g => g.Priority).ThenBy(g => g.Name)
            .ToListAsync(ct);

        var groupIds = groups.Select(g => g.Id).ToList();
        var counts = await db.Set<MixMatchProduct>().AsNoTracking()
            .Where(p => groupIds.Contains(p.MixMatchGroupId))
            .GroupBy(p => p.MixMatchGroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        return groups.Select(g => new MixMatchGroupDto(
            g.Id, g.Name, g.Type, g.Type.ToString(),
            g.Quantity, g.DiscountValue,
            g.StartDate, g.EndDate, g.DaysOfWeek, g.StartTime, g.EndTime,
            g.Priority, g.Stackable,
            counts.GetValueOrDefault(g.Id),
            g.IsActive)).ToList();
    }
}
