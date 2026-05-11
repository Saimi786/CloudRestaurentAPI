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

public sealed record UpdateProductCommand(
    Guid Id,
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

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Barcode).MaximumLength(100);
        RuleFor(x => x.BasePriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BasePriceCurrency).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
        RuleFor(x => x.CostPriceAmount).GreaterThanOrEqualTo(0).When(x => x.CostPriceAmount.HasValue);
        RuleFor(x => x.CostPriceCurrency).Length(3).Matches("^[A-Z]{3}$")
            .When(x => !string.IsNullOrEmpty(x.CostPriceCurrency));
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.HsnCode).MaximumLength(50);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0).When(x => x.ReorderPoint.HasValue);
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue);
    }
}

public sealed class UpdateProductHandler(IAppDbContext db) : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("Product", request.Id);

        if (!await db.Set<Category>().AnyAsync(c => c.Id == request.CategoryId, ct))
            throw new NotFoundException("Category", request.CategoryId);
        if (!await db.Set<Unit>().AnyAsync(u => u.Id == request.UnitId, ct))
            throw new NotFoundException("Unit", request.UnitId);
        if (request.BrandId is { } bid && !await db.Set<Brand>().AnyAsync(b => b.Id == bid, ct))
            throw new NotFoundException("Brand", bid);
        if (request.TaxRateId is { } trid && !await db.Set<TaxRate>().AnyAsync(t => t.Id == trid, ct))
            throw new NotFoundException("TaxRate", trid);
        if (await db.Set<Product>().AnyAsync(p => p.Id != request.Id && p.Sku == request.Sku, ct))
            throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");

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
        else
            product.SetCostPrice(null);

        var entry = db.Entry(product);
        entry.Property("CostPriceAmount").CurrentValue = product.CostPrice?.Amount;
        entry.Property("CostPriceCurrency").CurrentValue = product.CostPrice?.Currency;

        await db.SaveChangesAsync(ct);

        var costAmount = entry.Property("CostPriceAmount").CurrentValue as decimal?;
        var costCurrency = entry.Property("CostPriceCurrency").CurrentValue as string;

        return new ProductDto(
            product.Id, product.CategoryId, product.UnitId, product.BrandId, product.TaxRateId,
            product.Sku, product.Name, product.Description, product.Barcode,
            product.BasePrice.Amount, product.BasePrice.Currency,
            costAmount, costCurrency,
            (int)product.Type, product.Type.ToString(),
            product.ImageUrl, product.HsnCode, product.ReorderPoint, product.Weight,
            product.IsTaxable, product.IsSold, product.IsPurchased,
            product.IsActive, product.IsAvailable, product.IsStockTracked);
    }
}
