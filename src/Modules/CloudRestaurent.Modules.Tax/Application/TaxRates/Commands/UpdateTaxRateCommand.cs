using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tax.Application.TaxRates.Dtos;
using CloudRestaurent.Modules.Tax.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tax.Application.TaxRates.Commands;

public sealed record UpdateTaxRateCommand(
    Guid Id,
    string Name,
    decimal Percentage,
    bool IsCompound,
    bool IsDefault) : IRequest<TaxRateDto>;

public sealed class UpdateTaxRateValidator : AbstractValidator<UpdateTaxRateCommand>
{
    public UpdateTaxRateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Percentage).InclusiveBetween(0m, 100m);
    }
}

public sealed class UpdateTaxRateHandler(IAppDbContext db)
    : IRequestHandler<UpdateTaxRateCommand, TaxRateDto>
{
    public async Task<TaxRateDto> Handle(UpdateTaxRateCommand request, CancellationToken ct)
    {
        var rate = await db.Set<TaxRate>().FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("TaxRate", request.Id);

        if (await db.Set<TaxRate>().AnyAsync(t => t.Id != request.Id && t.Name == request.Name, ct))
            throw new ConflictException($"A tax rate named '{request.Name}' already exists.");

        rate.Update(request.Name, request.Percentage, request.IsCompound);

        if (request.IsDefault && !rate.IsDefault)
        {
            var existingDefaults = await db.Set<TaxRate>()
                .Where(t => t.Id != request.Id && t.IsDefault).ToListAsync(ct);
            foreach (var existing in existingDefaults) existing.UnmarkDefault();
            rate.MarkAsDefault();
        }
        else if (!request.IsDefault && rate.IsDefault)
        {
            rate.UnmarkDefault();
        }

        await db.SaveChangesAsync(ct);

        return new TaxRateDto(rate.Id, rate.Name, rate.Percentage, rate.IsCompound, rate.IsDefault, rate.IsActive);
    }
}
