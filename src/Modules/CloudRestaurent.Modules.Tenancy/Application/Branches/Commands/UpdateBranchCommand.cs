using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Branches.Dtos;
using CloudRestaurent.Domain.Companies;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Branches.Commands;

public sealed record UpdateBranchCommand(
    Guid Id,
    string Name,
    string Code,
    string? PhoneNumber,
    LocationDto Location,
    int? ReceiptTemplate = null,
    string? ReceiptFooterText = null) : IRequest<BranchDto>;

public sealed class UpdateBranchValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Z0-9-]+$");
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
        RuleFor(x => x.Location).NotNull();
        RuleFor(x => x.Location.TimeZone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ReceiptTemplate)
            .Must(v => v is null or 0 or 1)
            .WithMessage("ReceiptTemplate must be 0 (Compact) or 1 (Classic).");
        RuleFor(x => x.ReceiptFooterText).MaximumLength(500);
    }
}

public sealed class UpdateBranchHandler(IAppDbContext db)
    : IRequestHandler<UpdateBranchCommand, BranchDto>
{
    public async Task<BranchDto> Handle(UpdateBranchCommand request, CancellationToken ct)
    {
        // SuperAdmin is allowed to edit any tenant's branch, so bypass the per-tenant
        // global filter for the lookup; the controller has already gated this on the
        // SuperAdmin role so cross-tenant editing here is intentional.
        var branch = await db.Set<Branch>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == request.Id, ct)
            ?? throw new NotFoundException("Branch", request.Id);

        var codeTaken = await db.Set<Branch>().IgnoreQueryFilters().AnyAsync(b =>
            b.Id != request.Id &&
            b.CompanyId == branch.CompanyId &&
            b.Code == request.Code, ct);
        if (codeTaken)
            throw new ConflictException(
                $"A branch with code '{request.Code}' already exists in this company.");

        var location = new Location(
            request.Location.AddressLine1, request.Location.AddressLine2,
            request.Location.City, request.Location.State, request.Location.Country,
            request.Location.PostalCode, request.Location.Latitude, request.Location.Longitude,
            request.Location.TimeZone);

        branch.Update(request.Name, request.Code, location, request.PhoneNumber);
        if (request.ReceiptTemplate is not null || request.ReceiptFooterText is not null)
            branch.SetReceiptOptions(
                (ReceiptTemplate)(request.ReceiptTemplate ?? (int)branch.ReceiptTemplate),
                request.ReceiptFooterText ?? branch.ReceiptFooterText);
        await db.SaveChangesAsync(ct);

        return new BranchDto(
            branch.Id, branch.CompanyId, branch.Name, branch.Code, branch.PhoneNumber,
            new LocationDto(
                branch.Location.AddressLine1, branch.Location.AddressLine2,
                branch.Location.City, branch.Location.State, branch.Location.Country,
                branch.Location.PostalCode, branch.Location.Latitude, branch.Location.Longitude,
                branch.Location.TimeZone),
            branch.IsActive,
            (int)branch.ReceiptTemplate,
            branch.ReceiptFooterText);
    }
}
