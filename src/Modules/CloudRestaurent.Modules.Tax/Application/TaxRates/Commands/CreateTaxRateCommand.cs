using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tax.Application.TaxRates.Dtos;
using CloudRestaurent.Modules.Tax.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tax.Application.TaxRates.Commands;

public sealed record CreateTaxRateCommand(
    string Name,
    decimal Percentage,
    bool IsCompound = false,
    bool IsDefault = false) : IRequest<TaxRateDto>;

public sealed class CreateTaxRateValidator : AbstractValidator<CreateTaxRateCommand>
{
    public CreateTaxRateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Percentage).InclusiveBetween(0m, 100m);
    }
}

public sealed class CreateTaxRateHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateTaxRateCommand, TaxRateDto>
{
    public async Task<TaxRateDto> Handle(CreateTaxRateCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (await db.Set<TaxRate>().AnyAsync(t => t.Name == request.Name, ct))
            throw new ConflictException($"A tax rate named '{request.Name}' already exists.");

        var rate = new TaxRate(Guid.NewGuid(), tenantId, request.Name, request.Percentage, request.IsCompound);

        if (request.IsDefault)
        {
            // Only one default at a time — clear any existing default first.
            var existingDefaults = await db.Set<TaxRate>().Where(t => t.IsDefault).ToListAsync(ct);
            foreach (var existing in existingDefaults) existing.UnmarkDefault();
            rate.MarkAsDefault();
        }

        db.Set<TaxRate>().Add(rate);
        await db.SaveChangesAsync(ct);

        return new TaxRateDto(rate.Id, rate.Name, rate.Percentage, rate.IsCompound, rate.IsDefault, rate.IsActive);
    }
}
