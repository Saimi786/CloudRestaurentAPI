using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Queries;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto>;

public sealed class GetOrderByIdHandler(IAppDbContext db) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct)
            ?? throw new NotFoundException("Order", request.Id);
        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }
}
