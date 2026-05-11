using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Pricing.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Pricing.Application.Commands;

public sealed record DeactivatePriceRuleCommand(Guid Id) : IRequest;

public sealed class DeactivatePriceRuleHandler(IAppDbContext db) : IRequestHandler<DeactivatePriceRuleCommand>
{
    public async Task Handle(DeactivatePriceRuleCommand request, CancellationToken ct)
    {
        var rule = await db.Set<PriceRule>().FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("PriceRule", request.Id);
        if (!rule.IsActive) return;
        rule.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
