using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Products.Dtos;
using CloudRestaurent.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Brand = CloudRestaurent.Modules.Catalog.Domain.Brand;
using Category = CloudRestaurent.Modules.Catalog.Domain.Category;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using ProductType = CloudRestaurent.Modules.Catalog.Domain.ProductType;
using TaxRate = CloudRestaurent.Modules.Tax.Domain.TaxRate;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.Products.Commands;

public sealed record CreateProductCommand(
    Guid CategoryId,
    Guid UnitId,
    Guid? BrandId,
    Guid? TaxRateId,
    string Sku,
    string Name,
    string? Description,
    string? Barcode,
    decimal BasePriceAmount,
    string BasePriceCurrency,
    decimal? CostPriceAmount,
    string? CostPriceCurrency,
    ProductType Type = ProductType.Goods,
    string? ImageUrl = null,
    string? HsnCode = null,
    decimal? ReorderPoint = null,
    decimal? Weight = null,
    bool IsTaxable = true,
    bool IsSold = true,
    bool IsPurchased = true,
    bool IsStockTracked = false) : IRequest<ProductDto>;

public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Barcode).MaximumLength(100);
        RuleFor(x => x.BasePriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BasePriceCurrency).NotEmpty().Length(3).Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO 4217 code.");
        RuleFor(x => x.CostPriceAmount).GreaterThanOrEqualTo(0).When(x => x.CostPriceAmount.HasValue);
        RuleFor(x => x.CostPriceCurrency).Length(3).Matches("^[A-Z]{3}$")
            .When(x => !string.IsNullOrEmpty(x.CostPriceCurrency));
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.HsnCode).MaximumLength(50);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0).When(x => x.ReorderPoint.HasValue);
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue);
    }
}

public sealed class CreateProductHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (!await db.Set<Category>().AnyAsync(c => c.Id == request.CategoryId, ct))
            throw new NotFoundException("Category", request.CategoryId);
        if (!await db.Set<Unit>().AnyAsync(u => u.Id == request.UnitId, ct))
            throw new NotFoundException("Unit", request.UnitId);
        if (request.BrandId is { } bid && !await db.Set<Brand>().AnyAsync(b => b.Id == bid, ct))
            throw new NotFoundException("Brand", bid);
        if (request.TaxRateId is { } trid && !await db.Set<TaxRate>().AnyAsync(t => t.Id == trid, ct))
            throw new NotFoundException("TaxRate", trid);
        if (await db.Set<Product>().AnyAsync(p => p.Sku == request.Sku, ct))
            throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");

        var product = new Product(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            categoryId: request.CategoryId,
            unitId: request.UnitId,
            sku: request.Sku,
            name: request.Name,
            basePrice: new Money(request.BasePriceAmount, request.BasePriceCurrency));

        product.Update(
            request.CategoryId, request.UnitId, request.Sku, request.Name,
            request.Description, request.Barcode,
            new Money(request.BasePriceAmount, request.BasePriceCurrency),
            request.IsStockTracked);

        product.SetBrand(request.BrandId);
        product.SetTaxRate(request.TaxRateId);
        product.SetType(request.Type);
        product.SetImage(request.ImageUrl);
        product.SetHsnCode(request.HsnCode);
        product.SetReorderPoint(request.ReorderPoint);
        product.SetWeight(request.Weight);
        product.SetTaxable(request.IsTaxable);
        product.SetSoldPurchased(request.IsSold, request.IsPurchased);

        if (request.CostPriceAmount.HasValue && !string.IsNullOrEmpty(request.CostPriceCurrency))
            product.SetCostPrice(new Money(request.CostPriceAmount.Value, request.CostPriceCurrency));

        var entry = db.Set<Product>().Add(product);

        // Persist the shadow CostPrice columns (Product.CostPrice is Ignore'd in EF — see ProductConfiguration).
        if (product.CostPrice is { } cp)
        {
            entry.Property("CostPriceAmount").CurrentValue = cp.Amount;
            entry.Property("CostPriceCurrency").CurrentValue = cp.Currency;
        }

        await db.SaveChangesAsync(ct);

        return BuildDto(product, entry);
    }

    private static ProductDto BuildDto(Product p, Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Product> entry)
    {
        var costAmount = entry.Property("CostPriceAmount").CurrentValue as decimal?;
        var costCurrency = entry.Property("CostPriceCurrency").CurrentValue as string;
        return new ProductDto(
            p.Id, p.CategoryId, p.UnitId, p.BrandId, p.TaxRateId,
            p.Sku, p.Name, p.Description, p.Barcode,
            p.BasePrice.Amount, p.BasePrice.Currency,
            costAmount, costCurrency,
            (int)p.Type, p.Type.ToString(),
            p.ImageUrl, p.HsnCode, p.ReorderPoint, p.Weight,
            p.IsTaxable, p.IsSold, p.IsPurchased,
            p.IsActive, p.IsAvailable, p.IsStockTracked);
    }
}
