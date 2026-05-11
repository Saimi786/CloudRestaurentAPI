using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Units.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.Units.Commands;

public sealed record UpdateUnitCommand(
    Guid Id,
    Guid GroupId,
    string Code,
    string Name,
    decimal ConversionFactor) : IRequest<UnitDto>;

public sealed class UpdateUnitValidator : AbstractValidator<UpdateUnitCommand>
{
    public UpdateUnitValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10).Matches("^[A-Z0-9]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ConversionFactor).GreaterThan(0);
    }
}

public sealed class UpdateUnitHandler(IAppDbContext db) : IRequestHandler<UpdateUnitCommand, UnitDto>
{
    public async Task<UnitDto> Handle(UpdateUnitCommand request, CancellationToken ct)
    {
        var unit = await db.Set<Unit>().FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException("Unit", request.Id);

        var group = await db.Set<UnitGroup>().FirstOrDefaultAsync(g => g.Id == request.GroupId, ct)
            ?? throw new NotFoundException("UnitGroup", request.GroupId);

        if (await db.Set<Unit>().AnyAsync(u => u.Id != request.Id && u.Code == request.Code, ct))
            throw new ConflictException($"A unit with code '{request.Code}' already exists.");

        unit.Update(request.GroupId, request.Code, request.Name, request.ConversionFactor);
        await db.SaveChangesAsync(ct);

        return new UnitDto(unit.Id, unit.GroupId, group.Name,
            unit.Code, unit.Name, unit.ConversionFactor, unit.IsBase, unit.IsActive);
    }
}
