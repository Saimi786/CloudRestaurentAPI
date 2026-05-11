using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Brands.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Brands.Commands;

public sealed record CreateBrandCommand(
    string Name,
    string? Description,
    string? ImageUrl) : IRequest<BrandDto>;

public sealed class CreateBrandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public sealed class CreateBrandHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateBrandCommand, BrandDto>
{
    public async Task<BrandDto> Handle(CreateBrandCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (await db.Set<Brand>().AnyAsync(b => b.Name == request.Name, ct))
            throw new ConflictException($"A brand named '{request.Name}' already exists.");

        var brand = new Brand(Guid.NewGuid(), tenantId, request.Name, request.Description, request.ImageUrl);
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync(ct);

        return new BrandDto(brand.Id, brand.Name, brand.Description, brand.ImageUrl, brand.IsActive);
    }
}
