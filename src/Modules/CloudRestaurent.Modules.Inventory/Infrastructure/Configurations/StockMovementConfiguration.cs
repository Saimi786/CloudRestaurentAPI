using CloudRestaurent.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasConversion<int>();
        builder.Property(m => m.Quantity).HasPrecision(18, 6).IsRequired();
        builder.Property(m => m.QuantityInProductUnit).HasPrecision(18, 6).IsRequired();
        builder.Property(m => m.Reference).HasMaxLength(100);
        builder.Property(m => m.Notes).HasMaxLength(1000);

        // Most-common query: by product within a branch over a date range
        builder.HasIndex(m => new { m.TenantId, m.BranchId, m.ProductId, m.OccurredAt });
        builder.HasIndex(m => m.OccurredAt);
    }
}
