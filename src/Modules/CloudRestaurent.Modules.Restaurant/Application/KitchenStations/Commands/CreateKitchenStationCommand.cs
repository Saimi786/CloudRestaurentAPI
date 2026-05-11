using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Dtos;
using CloudRestaurent.Modules.Restaurant.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Commands;

public sealed record CreateKitchenStationCommand(
    Guid BranchId,
    string Name,
    int DisplayOrder,
    string? Description) : IRequest<KitchenStationDto>;

public sealed class CreateKitchenStationValidator : AbstractValidator<CreateKitchenStationCommand>
{
    public CreateKitchenStationValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateKitchenStationHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateKitchenStationCommand, KitchenStationDto>
{
    public async Task<KitchenStationDto> Handle(CreateKitchenStationCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var branch = await db.Set<Branch>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        if (await db.Set<KitchenStation>().AnyAsync(s =>
                s.BranchId == request.BranchId && s.Name == request.Name, ct))
            throw new ConflictException(
                $"A kitchen station named '{request.Name}' already exists at this branch.");

        var station = new KitchenStation(
            Guid.NewGuid(), tenantId, request.BranchId,
            request.Name, request.DisplayOrder, request.Description);
        db.Set<KitchenStation>().Add(station);
        await db.SaveChangesAsync(ct);

        return new KitchenStationDto(
            station.Id, station.BranchId, branch.Name,
            station.Name, station.DisplayOrder, station.Description, station.IsActive,
            station.PrinterIpAddress, station.PrinterPort);
    }
}
