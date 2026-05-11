using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Companies.Commands;

public sealed record DeactivateCompanyCommand(Guid Id) : IRequest;

public sealed class DeactivateCompanyHandler(IAppDbContext db)
    : IRequestHandler<DeactivateCompanyCommand>
{
    public async Task Handle(DeactivateCompanyCommand request, CancellationToken ct)
    {
        var company = await db.Set<Company>().FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException("Company", request.Id);

        if (!company.IsActive) return;

        company.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
