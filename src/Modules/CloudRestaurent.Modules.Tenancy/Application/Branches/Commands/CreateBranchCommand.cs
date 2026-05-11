using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Branches.Dtos;
using CloudRestaurent.Domain.Companies;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Branches.Commands;

public sealed record CreateBranchCommand(
    Guid CompanyId,
    string Name,
    string Code,
    string? PhoneNumber,
    LocationDto Location) : IRequest<BranchDto>;

public sealed class CreateBranchValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9-]+$")
            .WithMessage("Code may only contain uppercase letters, digits, and hyphens.");
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
        RuleFor(x => x.Location).NotNull();
        RuleFor(x => x.Location.TimeZone).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateBranchHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateBranchCommand, BranchDto>
{
    public async Task<BranchDto> Handle(CreateBranchCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var companyExists = await db.Set<Company>()
            .AnyAsync(c => c.Id == request.CompanyId, ct);
        if (!companyExists)
            throw new NotFoundException("Company", request.CompanyId);

        var codeTaken = await db.Set<Branch>()
            .AnyAsync(b => b.CompanyId == request.CompanyId && b.Code == request.Code, ct);
        if (codeTaken)
            throw new ConflictException(
                $"A branch with code '{request.Code}' already exists in this company.");

        var location = new Location(
            request.Location.AddressLine1, request.Location.AddressLine2,
            request.Location.City, request.Location.State, request.Location.Country,
            request.Location.PostalCode, request.Location.Latitude, request.Location.Longitude,
            request.Location.TimeZone);

        var branch = new Branch(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            companyId: request.CompanyId,
            name: request.Name,
            code: request.Code,
            location: location);
        branch.SetPhoneNumber(request.PhoneNumber);

        db.Set<Branch>().Add(branch);
        await db.SaveChangesAsync(ct);

        return ToDto(branch);
    }

    private static BranchDto ToDto(Branch b) => new(
        b.Id, b.CompanyId, b.Name, b.Code, b.PhoneNumber,
        new LocationDto(
            b.Location.AddressLine1, b.Location.AddressLine2,
            b.Location.City, b.Location.State, b.Location.Country,
            b.Location.PostalCode, b.Location.Latitude, b.Location.Longitude,
            b.Location.TimeZone),
        b.IsActive,
        (int)b.ReceiptTemplate,
        b.ReceiptFooterText);
}
