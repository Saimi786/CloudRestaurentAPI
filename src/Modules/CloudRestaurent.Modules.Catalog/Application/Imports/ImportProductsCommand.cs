using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Application.Common.Imports;
using CloudRestaurent.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Brand = CloudRestaurent.Modules.Catalog.Domain.Brand;
using Category = CloudRestaurent.Modules.Catalog.Domain.Category;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.Imports;

/// <summary>
/// Bulk-import products from CSV.
/// Expected header (case-insensitive): SKU,Name,Category,Unit,Brand,SalePrice,CostPrice,Barcode,Description
/// Category, Unit are required and looked up by name/code (created on first encounter? — no,
/// we error out instead so users don't accidentally seed garbage taxonomy).
/// SalePrice is required and must be a non-negative decimal. Currency defaults to PKR.
/// </summary>
public sealed record ImportProductsCommand(string CsvContent) : IRequest<ImportResultDto>;

public sealed class ImportProductsValidator : AbstractValidator<ImportProductsCommand>
{
    public ImportProductsValidator()
    {
        RuleFor(x => x.CsvContent).NotEmpty().WithMessage("CSV content is required.");
    }
}

public sealed class ImportProductsHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<ImportProductsCommand, ImportResultDto>
{
    private static readonly string[] RequiredColumns = ["SKU", "Name", "Category", "Unit", "SalePrice"];

    public async Task<ImportResultDto> Handle(ImportProductsCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var rows = CsvParser.Parse(request.CsvContent);
        if (rows.Count < 2)
            return new ImportResultDto(0, 0, 0,
                [new ImportRowError(0, "header", "CSV must include a header row and at least one data row.")]);

        var header = rows[0].Select(h => h.Trim()).ToList();
        var headerIdx = header
            .Select((h, i) => (h, i))
            .ToDictionary(p => p.h, p => p.i, StringComparer.OrdinalIgnoreCase);

        var missing = RequiredColumns.Where(c => !headerIdx.ContainsKey(c)).ToList();
        if (missing.Count > 0)
            return new ImportResultDto(rows.Count - 1, 0, rows.Count - 1,
                [new ImportRowError(0, "header", $"Missing required columns: {string.Join(", ", missing)}")]);

        // Pre-load lookup data once. Catalog imports are batch operations; we'd rather pull
        // every Category/Unit/Brand for the tenant up front than issue 3 queries per row.
        var categories = await db.Set<Category>().AsNoTracking()
            .Where(c => c.IsActive)
            .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);
        var units = await db.Set<Unit>().AsNoTracking()
            .ToDictionaryAsync(u => u.Code, u => u.Id, StringComparer.OrdinalIgnoreCase, ct);
        var brands = await db.Set<Brand>().AsNoTracking()
            .Where(b => b.IsActive)
            .ToDictionaryAsync(b => b.Name, b => b.Id, StringComparer.OrdinalIgnoreCase, ct);
        var existingSkus = await db.Set<Product>().AsNoTracking()
            .Select(p => p.Sku)
            .ToListAsync(ct);
        var skuSet = new HashSet<string>(existingSkus, StringComparer.OrdinalIgnoreCase);

        var errors = new List<ImportRowError>();
        var imported = 0;
        var data = rows.Skip(1).ToList();

        for (var rowIdx = 0; rowIdx < data.Count; rowIdx++)
        {
            var line = rowIdx + 2; // 1-based + header
            var row = data[rowIdx];
            string? Get(string col) => headerIdx.TryGetValue(col, out var i) && i < row.Count
                ? row[i]?.Trim() : null;

            var sku = Get("SKU");
            var name = Get("Name");
            var categoryName = Get("Category");
            var unitCode = Get("Unit");
            var priceRaw = Get("SalePrice");

            if (string.IsNullOrWhiteSpace(sku)) { errors.Add(new(line, "SKU", "SKU is required.")); continue; }
            if (string.IsNullOrWhiteSpace(name)) { errors.Add(new(line, "Name", "Name is required.")); continue; }
            if (string.IsNullOrWhiteSpace(categoryName)) { errors.Add(new(line, "Category", "Category is required.")); continue; }
            if (string.IsNullOrWhiteSpace(unitCode)) { errors.Add(new(line, "Unit", "Unit code is required.")); continue; }

            if (skuSet.Contains(sku))
            { errors.Add(new(line, "SKU", $"SKU '{sku}' already exists.")); continue; }
            if (!categories.TryGetValue(categoryName, out var categoryId))
            { errors.Add(new(line, "Category", $"Category '{categoryName}' not found. Create it first.")); continue; }
            if (!units.TryGetValue(unitCode, out var unitId))
            { errors.Add(new(line, "Unit", $"Unit code '{unitCode}' not found. Create it first.")); continue; }

            if (!decimal.TryParse(priceRaw, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var price) || price < 0)
            { errors.Add(new(line, "SalePrice", $"'{priceRaw}' is not a valid non-negative decimal.")); continue; }

            decimal? cost = null;
            var costRaw = Get("CostPrice");
            if (!string.IsNullOrWhiteSpace(costRaw))
            {
                if (!decimal.TryParse(costRaw, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var c) || c < 0)
                { errors.Add(new(line, "CostPrice", $"'{costRaw}' is not a valid non-negative decimal.")); continue; }
                cost = c;
            }

            Guid? brandId = null;
            var brandName = Get("Brand");
            if (!string.IsNullOrWhiteSpace(brandName))
            {
                if (!brands.TryGetValue(brandName, out var bid))
                { errors.Add(new(line, "Brand", $"Brand '{brandName}' not found. Create it first.")); continue; }
                brandId = bid;
            }

            var description = Get("Description");
            var barcode = Get("Barcode");

            var product = new Product(Guid.NewGuid(), tenantId, categoryId, unitId,
                sku, name, new Money(price, "PKR"));
            if (cost.HasValue) product.SetCostPrice(new Money(cost.Value, "PKR"));
            if (brandId.HasValue) product.SetBrand(brandId.Value);

            // Description/Barcode are only mutable via Update(); use it to seed both.
            product.Update(categoryId, unitId, sku, name,
                string.IsNullOrWhiteSpace(description) ? null : description,
                string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                new Money(price, "PKR"), isStockTracked: false);

            db.Set<Product>().Add(product);
            skuSet.Add(sku);
            imported++;
        }

        if (imported > 0) await db.SaveChangesAsync(ct);

        return new ImportResultDto(
            TotalRows: data.Count,
            ImportedRows: imported,
            SkippedRows: data.Count - imported,
            Errors: errors);
    }
}
