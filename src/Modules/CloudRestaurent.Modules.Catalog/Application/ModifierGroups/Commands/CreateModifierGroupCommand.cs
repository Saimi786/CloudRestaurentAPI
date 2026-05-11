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

public sealed record CreateModifierGroupCommand(
    string Name,
    bool IsRequired,
    int MinSelect,
    int MaxSelect,
    IReadOnlyList<ModifierInput> Modifiers) : IRequest<ModifierGroupDto>;

public sealed class CreateModifierGroupValidator : AbstractValidator<CreateModifierGroupCommand>
{
    public CreateModifierGroupValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.MaxSelect).GreaterThanOrEqualTo(1).WithMessage("Max select must be at least 1.");
        RuleFor(x => x.MinSelect).GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(x => x.MaxSelect).WithMessage("Min select cannot exceed max select.");
        RuleFor(x => x.MinSelect).GreaterThanOrEqualTo(1)
            .When(x => x.IsRequired)
            .WithMessage("Required groups must have min select ≥ 1.");
        RuleFor(x => x.Modifiers).NotNull().NotEmpty().WithMessage("A modifier group must have at least one modifier.");
        RuleForEach(x => x.Modifiers).ChildRules(m =>
        {
            m.RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            m.RuleFor(x => x.PriceAdjustmentCurrency).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
            m.RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class CreateModifierGroupHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateModifierGroupCommand, ModifierGroupDto>
{
    public async Task<ModifierGroupDto> Handle(CreateModifierGroupCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (await db.Set<ModifierGroup>().AnyAsync(g => g.Name == request.Name, ct))
            throw new ConflictException($"A modifier group named '{request.Name}' already exists.");

        var groupId = Guid.NewGuid();
        var modifiers = ModifierValidator.ValidateAndBuild(groupId, request.Modifiers);

        var group = new ModifierGroup(groupId, tenantId, request.Name.Trim(),
            request.IsRequired, request.MinSelect, request.MaxSelect);
        group.ReplaceModifiers(modifiers);

        db.Set<ModifierGroup>().Add(group);
        await db.SaveChangesAsync(ct);

        return await GetModifierGroupByIdHandler.BuildDtoAsync(db, group, ct);
    }
}
