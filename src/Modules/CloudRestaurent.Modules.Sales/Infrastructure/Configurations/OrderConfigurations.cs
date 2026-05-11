using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type).HasConversion<int>();
        builder.Property(o => o.Status).HasConversion<int>();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.OrderNumber).HasMaxLength(50);
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.SubtotalAmount).HasPrecision(18, 4);
        builder.Property(o => o.TaxAmount).HasPrecision(18, 4);
        builder.Property(o => o.DiscountAmount).HasPrecision(18, 4);
        builder.Property(o => o.PromotionDiscountAmount).HasPrecision(18, 4);
        builder.Property(o => o.GrandTotalAmount).HasPrecision(18, 4);
        builder.Property(o => o.RewardPointsRedeemedAmount).HasPrecision(18, 4);

        builder.HasIndex(o => new { o.TenantId, o.BranchId, o.Status });
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.OpenedAt);
        builder.HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique()
            .HasFilter("[OrderNumber] IS NOT NULL");

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Payments)
            .WithOne()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.ProductSku).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 6).IsRequired();
        builder.Property(l => l.Notes).HasMaxLength(500);
        builder.Property(l => l.LineSubtotal).HasPrecision(18, 4);
        builder.Property(l => l.TaxRatePercentage).HasPrecision(5, 2);
        builder.Property(l => l.TaxAmount).HasPrecision(18, 4);
        builder.Property(l => l.LineGrandTotal).HasPrecision(18, 4);

        builder.HasIndex(l => l.OrderId);
        builder.HasIndex(l => l.ProductId);
        builder.HasIndex(l => l.TaxRateId);

        builder.ComplexProperty(l => l.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("UnitPriceAmount").HasPrecision(18, 4).IsRequired();
            money.Property(m => m.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3).IsRequired();
        });

        builder.HasMany(l => l.Modifiers)
            .WithOne()
            .HasForeignKey(m => m.OrderLineId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrderLineModifierConfiguration : IEntityTypeConfiguration<OrderLineModifier>
{
    public void Configure(EntityTypeBuilder<OrderLineModifier> builder)
    {
        builder.ToTable("OrderLineModifiers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(m => m.OrderLineId);

        builder.ComplexProperty(m => m.PriceAdjustment, money =>
        {
            money.Property(p => p.Amount).HasColumnName("PriceAdjustmentAmount").HasPrecision(18, 4).IsRequired();
            money.Property(p => p.Currency).HasColumnName("PriceAdjustmentCurrency").HasMaxLength(3).IsRequired();
        });
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Method).HasConversion<int>();
        builder.Property(p => p.Reference).HasMaxLength(100);

        builder.HasIndex(p => p.OrderId);

        builder.ComplexProperty(p => p.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("AmountValue").HasPrecision(18, 4).IsRequired();
            money.Property(m => m.Currency).HasColumnName("AmountCurrency").HasMaxLength(3).IsRequired();
        });
    }
}
