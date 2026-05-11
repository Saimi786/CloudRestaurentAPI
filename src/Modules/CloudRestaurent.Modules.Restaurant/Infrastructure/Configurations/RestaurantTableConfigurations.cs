using CloudRestaurent.Modules.Restaurant.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class FloorPlanConfiguration : IEntityTypeConfiguration<FloorPlan>
{
    public void Configure(EntityTypeBuilder<FloorPlan> builder)
    {
        builder.ToTable("FloorPlans");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(f => new { f.TenantId, f.BranchId, f.Name }).IsUnique();
        builder.HasIndex(f => f.BranchId);
    }
}

public sealed class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("RestaurantTables");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Status).HasConversion<int>();

        // Code unique within a branch (across floor plans), not just within a floor plan,
        // so staff don't see two "T-12"s in the same outlet.
        builder.HasIndex(t => new { t.TenantId, t.BranchId, t.Code }).IsUnique();
        builder.HasIndex(t => t.FloorPlanId);
        builder.HasIndex(t => t.BranchId);
    }
}
