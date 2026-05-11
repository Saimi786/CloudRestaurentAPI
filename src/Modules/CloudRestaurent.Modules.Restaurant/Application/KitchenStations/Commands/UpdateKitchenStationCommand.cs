using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Dtos;
using CloudRestaurent.Modules.Restaurant.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Commands;

public sealed record UpdateKitchenStationCommand(
    Guid Id,
    string Name,
    int DisplayOrder,
    string? Description,
    string? PrinterIpAddress = null,
    int? PrinterPort = null) : IRequest<KitchenStationDto>;

public sealed class UpdateKitchenStationValidator : AbstractValidator<UpdateKitchenStationCommand>
{
    public UpdateKitchenStationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.PrinterIpAddress).MaximumLength(50);
        RuleFor(x => x.PrinterPort).InclusiveBetween(1, 65535).When(x => x.PrinterPort.HasValue);
    }
}

public sealed class UpdateKitchenStationHandler(IAppDbContext db)
    : IRequestHandler<UpdateKitchenStationCommand, KitchenStationDto>
{
    public async Task<KitchenStationDto> Handle(UpdateKitchenStationCommand request, CancellationToken ct)
    {
        var station = await db.Set<KitchenStation>().FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new NotFoundException("KitchenStation", request.Id);

        if (await db.Set<KitchenStation>().AnyAsync(s =>
                s.Id != request.Id &&
                s.BranchId == station.BranchId &&
                s.Name == request.Name, ct))
            throw new ConflictException($"A kitchen station named '{request.Name}' already exists at this branch.");

        station.Update(request.Name, request.DisplayOrder, request.Description);
        // Treat empty string as "clear" so admins can unset a stale IP without deleting the station.
        station.SetPrinter(
            string.IsNullOrWhiteSpace(request.PrinterIpAddress) ? null : request.PrinterIpAddress.Trim(),
            request.PrinterPort);
        await db.SaveChangesAsync(ct);

        var branchName = await db.Set<Branch>().AsNoTracking()
            .Where(b => b.Id == station.BranchId).Select(b => b.Name).FirstAsync(ct);

        return new KitchenStationDto(
            station.Id, station.BranchId, branchName,
            station.Name, station.DisplayOrder, station.Description, station.IsActive,
            station.PrinterIpAddress, station.PrinterPort);
    }
}
