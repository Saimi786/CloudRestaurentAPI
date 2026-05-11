using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class ModifierGroupConfiguration : IEntityTypeConfiguration<ModifierGroup>
{
    public void Configure(EntityTypeBuilder<ModifierGroup> builder)
    {
        builder.ToTable("ModifierGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();

        builder.HasMany(g => g.Modifiers)
            .WithOne()
            .HasForeignKey(m => m.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // No AutoInclude — read paths query explicitly, write paths use ExecuteDeleteAsync.
    }
}

public sealed class ModifierConfiguration : IEntityTypeConfiguration<Modifier>
{
    public void Configure(EntityTypeBuilder<Modifier> builder)
    {
        builder.ToTable("Modifiers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(m => m.ModifierGroupId);
        builder.HasIndex(m => new { m.ModifierGroupId, m.Name }).IsUnique();

        builder.ComplexProperty(m => m.PriceAdjustment, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("PriceAdjustmentAmount")
                .HasPrecision(18, 4)
                .IsRequired();
            money.Property(x => x.Currency)
                .HasColumnName("PriceAdjustmentCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}

public sealed class ProductModifierGroupConfiguration : IEntityTypeConfiguration<ProductModifierGroup>
{
    public void Configure(EntityTypeBuilder<ProductModifierGroup> builder)
    {
        builder.ToTable("ProductModifierGroups");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.TenantId, p.ProductId, p.ModifierGroupId }).IsUnique();
        builder.HasIndex(p => p.ProductId);
        builder.HasIndex(p => p.ModifierGroupId);
    }
}
