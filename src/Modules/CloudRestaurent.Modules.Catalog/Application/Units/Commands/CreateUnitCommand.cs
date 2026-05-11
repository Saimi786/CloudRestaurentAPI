using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Units.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.Units.Commands;

public sealed record CreateUnitCommand(
    Guid GroupId,
    string Code,
    string Name,
    decimal ConversionFactor) : IRequest<UnitDto>;

public sealed class CreateUnitValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10).Matches("^[A-Z0-9]+$")
            .WithMessage("Code must be uppercase letters and digits only.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ConversionFactor).GreaterThan(0)
            .WithMessage("Conversion factor must be greater than 0.");
    }
}

public sealed class CreateUnitHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateUnitCommand, UnitDto>
{
    public async Task<UnitDto> Handle(CreateUnitCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var group = await db.Set<UnitGroup>().FirstOrDefaultAsync(g => g.Id == request.GroupId, ct)
            ?? throw new NotFoundException("UnitGroup", request.GroupId);

        if (await db.Set<Unit>().AnyAsync(u => u.Code == request.Code, ct))
            throw new ConflictException($"A unit with code '{request.Code}' already exists.");

        var unit = new Unit(Guid.NewGuid(), tenantId, request.GroupId,
            request.Code, request.Name, request.ConversionFactor);
        db.Set<Unit>().Add(unit);
        await db.SaveChangesAsync(ct);

        return new UnitDto(unit.Id, unit.GroupId, group.Name,
            unit.Code, unit.Name, unit.ConversionFactor, unit.IsBase, unit.IsActive);
    }
}
