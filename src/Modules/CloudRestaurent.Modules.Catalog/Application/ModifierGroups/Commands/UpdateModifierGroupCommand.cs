using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Common;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Queries;
using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Commands;

public sealed record UpdateModifierGroupCommand(
    Guid Id,
    string Name,
    bool IsRequired,
    int MinSelect,
    int MaxSelect,
    IReadOnlyList<ModifierInput> Modifiers) : IRequest<ModifierGroupDto>;

public sealed class UpdateModifierGroupValidator : AbstractValidator<UpdateModifierGroupCommand>
{
    public UpdateModifierGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MaxSelect).GreaterThanOrEqualTo(1);
        RuleFor(x => x.MinSelect).GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(x => x.MaxSelect);
        RuleFor(x => x.MinSelect).GreaterThanOrEqualTo(1).When(x => x.IsRequired);
        RuleFor(x => x.Modifiers).NotNull().NotEmpty();
        RuleForEach(x => x.Modifiers).ChildRules(m =>
        {
            m.RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            m.RuleFor(x => x.PriceAdjustmentCurrency).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
            m.RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateModifierGroupHandler(IAppDbContext db)
    : IRequestHandler<UpdateModifierGroupCommand, ModifierGroupDto>
{
    public async Task<ModifierGroupDto> Handle(UpdateModifierGroupCommand request, CancellationToken ct)
    {
        var group = await db.Set<ModifierGroup>().FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("ModifierGroup", request.Id);

        if (await db.Set<ModifierGroup>().AnyAsync(g => g.Id != request.Id && g.Name == request.Name, ct))
            throw new ConflictException($"A modifier group named '{request.Name}' already exists.");

        // Wipe old modifier rows directly in SQL — bypasses change tracker, avoids the
        // AutoInclude-vs-RemoveRange concurrency issue we hit on Recipes.
        await db.Set<Modifier>()
            .Where(m => m.ModifierGroupId == group.Id)
            .ExecuteDeleteAsync(ct);

        var newModifiers = ModifierValidator.ValidateAndBuild(group.Id, request.Modifiers);

        group.Update(request.Name.Trim(), request.IsRequired, request.MinSelect, request.MaxSelect);
        db.Set<Modifier>().AddRange(newModifiers);

        await db.SaveChangesAsync(ct);

        return await GetModifierGroupByIdHandler.BuildDtoAsync(db, group, ct);
    }
}
