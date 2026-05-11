using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Queries;

public sealed record GetModifierGroupByIdQuery(Guid Id) : IRequest<ModifierGroupDto>;

public sealed class GetModifierGroupByIdHandler(IAppDbContext db)
    : IRequestHandler<GetModifierGroupByIdQuery, ModifierGroupDto>
{
    public async Task<ModifierGroupDto> Handle(GetModifierGroupByIdQuery request, CancellationToken ct)
    {
        var group = await db.Set<ModifierGroup>().AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("ModifierGroup", request.Id);

        return await BuildDtoAsync(db, group, ct);
    }

    internal static async Task<ModifierGroupDto> BuildDtoAsync(IAppDbContext db, ModifierGroup group, CancellationToken ct)
    {
        var modifiers = await db.Set<Modifier>().AsNoTracking()
            .Where(m => m.ModifierGroupId == group.Id)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
            .Select(m => new ModifierDto(
                m.Id, m.Name,
                m.PriceAdjustment.Amount, m.PriceAdjustment.Currency,
                m.DisplayOrder, m.IsDefault))
            .ToListAsync(ct);

        return new ModifierGroupDto(
            group.Id, group.Name, group.IsRequired,
            group.MinSelect, group.MaxSelect, group.IsActive,
            modifiers);
    }
}
