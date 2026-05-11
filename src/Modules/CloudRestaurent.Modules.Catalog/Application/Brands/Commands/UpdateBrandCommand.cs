using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Brands.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Brands.Commands;

public sealed record UpdateBrandCommand(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl) : IRequest<BrandDto>;

public sealed class UpdateBrandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public sealed class UpdateBrandHandler(IAppDbContext db)
    : IRequestHandler<UpdateBrandCommand, BrandDto>
{
    public async Task<BrandDto> Handle(UpdateBrandCommand request, CancellationToken ct)
    {
        var brand = await db.Set<Brand>().FirstOrDefaultAsync(b => b.Id == request.Id, ct)
            ?? throw new NotFoundException("Brand", request.Id);

        if (await db.Set<Brand>().AnyAsync(b => b.Id != request.Id && b.Name == request.Name, ct))
            throw new ConflictException($"A brand named '{request.Name}' already exists.");

        brand.Update(request.Name, request.Description, request.ImageUrl);
        await db.SaveChangesAsync(ct);

        return new BrandDto(brand.Id, brand.Name, brand.Description, brand.ImageUrl, brand.IsActive);
    }
}
