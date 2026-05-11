using CloudRestaurent.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(po => po.Id);

        builder.Property(po => po.Number).HasMaxLength(20).IsRequired();
        builder.Property(po => po.Currency).HasMaxLength(3).IsRequired();
        builder.Property(po => po.Status).HasConversion<int>();
        builder.Property(po => po.Notes).HasMaxLength(2000);
        builder.Property(po => po.SubtotalAmount).HasPrecision(18, 4);
        builder.Property(po => po.TaxAmount).HasPrecision(18, 4);
        builder.Property(po => po.GrandTotalAmount).HasPrecision(18, 4);

        builder.HasIndex(po => new { po.TenantId, po.Number }).IsUnique();
        builder.HasIndex(po => po.SupplierId);
        builder.HasIndex(po => po.Status);

        builder.HasMany(po => po.Lines)
            .WithOne()
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProductSku).HasMaxLength(60).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.OrderedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasIndex(l => l.PurchaseOrderId);
        builder.HasIndex(l => l.ProductId);
    }
}

public sealed class SupplierBillConfiguration : IEntityTypeConfiguration<SupplierBill>
{
    public void Configure(EntityTypeBuilder<SupplierBill> builder)
    {
        builder.ToTable("SupplierBills");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Number).HasMaxLength(20).IsRequired();
        builder.Property(b => b.SupplierBillReference).HasMaxLength(60);
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired();
        builder.Property(b => b.Status).HasConversion<int>();
        builder.Property(b => b.Notes).HasMaxLength(1000);
        builder.Property(b => b.Amount).HasPrecision(18, 4);
        builder.Property(b => b.PaidAmount).HasPrecision(18, 4);
        builder.Property(b => b.MatchStatus).HasConversion<int>();
        builder.Property(b => b.ExpectedAmount).HasPrecision(18, 4);
        builder.Property(b => b.DiscrepancyAmount).HasPrecision(18, 4);
        builder.Property(b => b.DiscrepancyReason).HasMaxLength(1000);

        builder.HasIndex(b => new { b.TenantId, b.Number }).IsUnique();
        builder.HasIndex(b => b.MatchStatus);
        builder.HasIndex(b => b.SupplierId);
        builder.HasIndex(b => b.PurchaseOrderId);
        builder.HasIndex(b => b.Status);

        builder.HasMany(b => b.Payments)
            .WithOne()
            .HasForeignKey(p => p.BillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SupplierBillPaymentConfiguration : IEntityTypeConfiguration<SupplierBillPayment>
{
    public void Configure(EntityTypeBuilder<SupplierBillPayment> builder)
    {
        builder.ToTable("SupplierBillPayments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.Method).HasConversion<int>();
        builder.Property(p => p.Reference).HasMaxLength(120);

        builder.HasIndex(p => p.BillId);
    }
}
