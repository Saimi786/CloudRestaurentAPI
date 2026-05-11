using CloudRestaurent.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class UnitGroupConfiguration : IEntityTypeConfiguration<UnitGroup>
{
    public void Configure(EntityTypeBuilder<UnitGroup> builder)
    {
        builder.ToTable("UnitGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();
    }
}
