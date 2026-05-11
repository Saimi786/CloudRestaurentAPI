using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Dtos;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Queries;

public sealed record GetKitchenStationByIdQuery(Guid Id) : IRequest<KitchenStationDto>;

public sealed class GetKitchenStationByIdHandler(IAppDbContext db)
    : IRequestHandler<GetKitchenStationByIdQuery, KitchenStationDto>
{
    public async Task<KitchenStationDto> Handle(GetKitchenStationByIdQuery request, CancellationToken ct)
    {
        var dto = await (
            from s in db.Set<KitchenStation>().AsNoTracking()
            join br in db.Set<Branch>().AsNoTracking() on s.BranchId equals br.Id
            where s.Id == request.Id
            select new KitchenStationDto(
                s.Id, s.BranchId, br.Name,
                s.Name, s.DisplayOrder, s.Description, s.IsActive,
                s.PrinterIpAddress, s.PrinterPort))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("KitchenStation", request.Id);
        return dto;
    }
}
