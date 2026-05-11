using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Queries;

public sealed record GetModifierGroupsQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<ModifierGroupSummaryDto>>;

public sealed class GetModifierGroupsHandler(IAppDbContext db)
    : IRequestHandler<GetModifierGroupsQuery, IReadOnlyList<ModifierGroupSummaryDto>>
{
    public async Task<IReadOnlyList<ModifierGroupSummaryDto>> Handle(GetModifierGroupsQuery request, CancellationToken ct)
    {
        var groups = db.Set<ModifierGroup>().AsNoTracking();
        if (!request.IncludeInactive) groups = groups.Where(g => g.IsActive);

        return await groups
            .OrderBy(g => g.Name)
            .Select(g => new ModifierGroupSummaryDto(
                g.Id, g.Name, g.IsRequired, g.MinSelect, g.MaxSelect,
                db.Set<Modifier>().Count(m => m.ModifierGroupId == g.Id),
                g.IsActive))
            .ToListAsync(ct);
    }
}
