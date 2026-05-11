using CloudRestaurent.Modules.Pricing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class MixMatchGroupConfiguration : IEntityTypeConfiguration<MixMatchGroup>
{
    public void Configure(EntityTypeBuilder<MixMatchGroup> builder)
    {
        builder.ToTable("MixMatchGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).HasMaxLength(150).IsRequired();
        builder.Property(g => g.Type).HasConversion<int>();
        builder.Property(g => g.DaysOfWeek).HasConversion<int>();
        builder.Property(g => g.DiscountValue).HasPrecision(18, 4);

        builder.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();
        builder.HasIndex(g => g.IsActive);

        builder.HasMany(g => g.Products)
            .WithOne()
            .HasForeignKey(p => p.MixMatchGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MixMatchProductConfiguration : IEntityTypeConfiguration<MixMatchProduct>
{
    public void Configure(EntityTypeBuilder<MixMatchProduct> builder)
    {
        builder.ToTable("MixMatchProducts");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.MixMatchGroupId);
        builder.HasIndex(p => p.ProductId);
        builder.HasIndex(p => new { p.MixMatchGroupId, p.ProductId }).IsUnique();
    }
}
