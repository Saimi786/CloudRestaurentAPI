using CloudRestaurent.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Modules.Catalog.Infrastructure.Configurations;

public sealed class ComboComponentConfiguration : IEntityTypeConfiguration<ComboComponent>
{
    public void Configure(EntityTypeBuilder<ComboComponent> builder)
    {
        builder.ToTable("ComboComponents");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Quantity).HasPrecision(18, 6);
        builder.HasIndex(c => new { c.TenantId, c.ParentProductId, c.ComponentProductId }).IsUnique();
    }
}
