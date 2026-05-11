using CloudRestaurent.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalances");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Quantity).HasPrecision(18, 6).IsRequired();

        builder.HasIndex(b => new { b.TenantId, b.BranchId, b.ProductId }).IsUnique();
        builder.HasIndex(b => b.ProductId);
    }
}
