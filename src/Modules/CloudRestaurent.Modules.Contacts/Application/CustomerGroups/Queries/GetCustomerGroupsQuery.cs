using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Queries;

public sealed record GetCustomerGroupsQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<CustomerGroupDto>>;

public sealed class GetCustomerGroupsHandler(IAppDbContext db)
    : IRequestHandler<GetCustomerGroupsQuery, IReadOnlyList<CustomerGroupDto>>
{
    public async Task<IReadOnlyList<CustomerGroupDto>> Handle(GetCustomerGroupsQuery request, CancellationToken ct)
    {
        var query = db.Set<CustomerGroup>().AsNoTracking();
        if (!request.IncludeInactive)
            query = query.Where(g => g.IsActive);

        return await query
            .OrderBy(g => g.Name)
            .Select(g => new CustomerGroupDto(g.Id, g.Name, g.DiscountPercent, g.Description, g.IsActive))
            .ToListAsync(ct);
    }
}
