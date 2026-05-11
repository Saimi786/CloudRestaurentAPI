using CloudRestaurent.Modules.Restaurant.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class KitchenStationConfiguration : IEntityTypeConfiguration<KitchenStation>
{
    public void Configure(EntityTypeBuilder<KitchenStation> builder)
    {
        builder.ToTable("KitchenStations");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.PrinterIpAddress).HasMaxLength(50);

        builder.HasIndex(s => new { s.TenantId, s.BranchId, s.Name }).IsUnique();
        builder.HasIndex(s => s.BranchId);
    }
}
