using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.UnitGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.UnitGroups.Commands;

public sealed record CreateUnitGroupCommand(string Name) : IRequest<UnitGroupDto>;

public sealed class CreateUnitGroupValidator : AbstractValidator<CreateUnitGroupCommand>
{
    public CreateUnitGroupValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateUnitGroupHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateUnitGroupCommand, UnitGroupDto>
{
    public async Task<UnitGroupDto> Handle(CreateUnitGroupCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (await db.Set<UnitGroup>().AnyAsync(g => g.Name == request.Name, ct))
            throw new ConflictException($"A unit group named '{request.Name}' already exists.");

        var group = new UnitGroup(Guid.NewGuid(), tenantId, request.Name);
        db.Set<UnitGroup>().Add(group);
        await db.SaveChangesAsync(ct);

        return new UnitGroupDto(group.Id, group.Name, 0, group.IsActive);
    }
}
