using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Pricing.Application.Dtos;
using MediatR;

namespace CloudRestaurent.Modules.Pricing.Application.Queries;

public sealed record ResolvePriceQuery(Guid ProductId, Guid? BranchId, DateTime? AtLocal)
    : IRequest<ResolvedPriceDto>;

public sealed class ResolvePriceHandler(IPriceResolver resolver)
    : IRequestHandler<ResolvePriceQuery, ResolvedPriceDto>
{
    public async Task<ResolvedPriceDto> Handle(ResolvePriceQuery request, CancellationToken ct)
    {
        var at = request.AtLocal ?? DateTime.Now;
        var resolved = await resolver.ResolveAsync(request.ProductId, request.BranchId, at, ct);
        return new ResolvedPriceDto(
            resolved.Amount, resolved.Currency,
            resolved.AppliedRuleId, resolved.AppliedRuleName);
    }
}
