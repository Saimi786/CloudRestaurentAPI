using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Queries;

public sealed record GetCustomerGroupByIdQuery(Guid Id) : IRequest<CustomerGroupDto>;

public sealed class GetCustomerGroupByIdHandler(IAppDbContext db)
    : IRequestHandler<GetCustomerGroupByIdQuery, CustomerGroupDto>
{
    public async Task<CustomerGroupDto> Handle(GetCustomerGroupByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Set<CustomerGroup>().AsNoTracking()
            .Where(g => g.Id == request.Id)
            .Select(g => new CustomerGroupDto(g.Id, g.Name, g.DiscountPercent, g.Description, g.IsActive))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("CustomerGroup", request.Id);
        return dto;
    }
}
