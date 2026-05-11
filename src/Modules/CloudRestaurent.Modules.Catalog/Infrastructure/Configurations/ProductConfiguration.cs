using CloudRestaurent.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Barcode).HasMaxLength(100);
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.HsnCode).HasMaxLength(50);
        builder.Property(p => p.ReorderPoint).HasPrecision(18, 4);
        builder.Property(p => p.Weight).HasPrecision(18, 4);
        builder.Property(p => p.Type).HasConversion<int>();

        builder.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
        builder.HasIndex(p => p.Barcode);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.UnitId);
        builder.HasIndex(p => p.BrandId);
        builder.HasIndex(p => p.TaxRateId);

        builder.ComplexProperty(p => p.BasePrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("BasePriceAmount")
                .HasPrecision(18, 4)
                .IsRequired();
            money.Property(m => m.Currency)
                .HasColumnName("BasePriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // CostPrice is optional (Money?). EF Core 8+ supports complex properties on
        // nullable struct types only when wired as ComplexProperty with all sub-properties
        // mapped to nullable columns. Easier: keep as required complex with default zero,
        // and surface "no cost tracked" via a sentinel currency check at the read layer.
        // For now: split into discrete columns to keep the optional semantics clean.
        builder.Property<decimal?>("CostPriceAmount").HasPrecision(18, 4);
        builder.Property<string?>("CostPriceCurrency").HasMaxLength(3);
        builder.Ignore(p => p.CostPrice);
    }
}
