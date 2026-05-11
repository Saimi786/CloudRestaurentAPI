using CloudRestaurent.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class WasteLogConfiguration : IEntityTypeConfiguration<WasteLog>
{
    public void Configure(EntityTypeBuilder<WasteLog> builder)
    {
        builder.ToTable("WasteLogs");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Quantity).HasPrecision(18, 4);
        builder.Property(w => w.QuantityInProductUnit).HasPrecision(18, 4);
        builder.Property(w => w.Notes).HasMaxLength(1000);
        builder.Property(w => w.Reason).HasConversion<int>();

        builder.HasIndex(w => new { w.TenantId, w.OccurredAt });
        builder.HasIndex(w => w.BranchId);
        builder.HasIndex(w => w.ProductId);
        builder.HasIndex(w => w.Reason);
    }
}
