using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tax.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tax.Application.TaxRates.Commands;

public sealed record DeactivateTaxRateCommand(Guid Id) : IRequest;

public sealed class DeactivateTaxRateHandler(IAppDbContext db)
    : IRequestHandler<DeactivateTaxRateCommand>
{
    public async Task Handle(DeactivateTaxRateCommand request, CancellationToken ct)
    {
        var rate = await db.Set<TaxRate>().FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("TaxRate", request.Id);
        if (!rate.IsActive) return;

        if (rate.IsDefault)
            throw new BusinessRuleException(
                "Cannot deactivate the default tax rate. Mark another rate as default first.");

        rate.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
