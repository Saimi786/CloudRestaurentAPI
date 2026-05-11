using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain;

public class Product : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid CategoryId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid? BrandId { get; private set; }
    public Guid? TaxRateId { get; private set; }
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Barcode { get; private set; }
    public Money BasePrice { get; private set; }
    public Money? CostPrice { get; private set; }
    public ProductType Type { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? HsnCode { get; private set; }
    public decimal? ReorderPoint { get; private set; }
    public decimal? Weight { get; private set; }
    public bool IsTaxable { get; private set; }
    public bool IsSold { get; private set; }
    public bool IsPurchased { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsStockTracked { get; private set; }

    private Product() { }

    public Product(
        Guid id,
        Guid tenantId,
        Guid categoryId,
        Guid unitId,
        string sku,
        string name,
        Money basePrice)
    {
        Id = id;
        TenantId = tenantId;
        CategoryId = categoryId;
        UnitId = unitId;
        Sku = sku;
        Name = name;
        BasePrice = basePrice;
        Type = ProductType.Goods;
        IsTaxable = true;
        IsSold = true;
        IsPurchased = true;
        IsActive = true;
        IsAvailable = true;
        IsStockTracked = false;
    }

    public void Update(
        Guid categoryId,
        Guid unitId,
        string sku,
        string name,
        string? description,
        string? barcode,
        Money basePrice,
        bool isStockTracked)
    {
        CategoryId = categoryId;
        UnitId = unitId;
        Sku = sku;
        Name = name;
        Description = description;
        Barcode = barcode;
        BasePrice = basePrice;
        IsStockTracked = isStockTracked;
    }

    public void SetCostPrice(Money? costPrice) => CostPrice = costPrice;
    public void SetBrand(Guid? brandId) => BrandId = brandId;
    public void SetTaxRate(Guid? taxRateId) => TaxRateId = taxRateId;
    public void SetType(ProductType type) => Type = type;
    public void SetImage(string? imageUrl) => ImageUrl = imageUrl;
    public void SetHsnCode(string? hsnCode) => HsnCode = hsnCode;
    public void SetReorderPoint(decimal? reorderPoint) => ReorderPoint = reorderPoint;
    public void SetWeight(decimal? weight) => Weight = weight;
    public void SetTaxable(bool taxable) => IsTaxable = taxable;
    public void SetSoldPurchased(bool isSold, bool isPurchased)
    {
        IsSold = isSold;
        IsPurchased = isPurchased;
    }
    public void SetStockTracked(bool tracked) => IsStockTracked = tracked;
    public void SetAvailability(bool available) => IsAvailable = available;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
