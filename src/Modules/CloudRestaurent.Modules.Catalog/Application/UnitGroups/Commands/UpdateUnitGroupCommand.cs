using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.UnitGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainUnit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.UnitGroups.Commands;

public sealed record UpdateUnitGroupCommand(Guid Id, string Name) : IRequest<UnitGroupDto>;

public sealed class UpdateUnitGroupValidator : AbstractValidator<UpdateUnitGroupCommand>
{
    public UpdateUnitGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateUnitGroupHandler(IAppDbContext db)
    : IRequestHandler<UpdateUnitGroupCommand, UnitGroupDto>
{
    public async Task<UnitGroupDto> Handle(UpdateUnitGroupCommand request, CancellationToken ct)
    {
        var group = await db.Set<UnitGroup>().FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("UnitGroup", request.Id);

        if (await db.Set<UnitGroup>().AnyAsync(g => g.Id != request.Id && g.Name == request.Name, ct))
            throw new ConflictException($"A unit group named '{request.Name}' already exists.");

        group.Update(request.Name);
        await db.SaveChangesAsync(ct);

        var unitCount = await db.Set<DomainUnit>().CountAsync(u => u.GroupId == group.Id, ct);
        return new UnitGroupDto(group.Id, group.Name, unitCount, group.IsActive);
    }
}
