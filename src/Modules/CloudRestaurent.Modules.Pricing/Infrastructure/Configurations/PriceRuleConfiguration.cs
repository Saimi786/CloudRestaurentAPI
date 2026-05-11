using CloudRestaurent.Modules.Pricing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class PriceRuleConfiguration : IEntityTypeConfiguration<PriceRule>
{
    public void Configure(EntityTypeBuilder<PriceRule> builder)
    {
        builder.ToTable("PriceRules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();
        builder.Property(r => r.DaysOfWeek).HasConversion<int>();

        builder.HasIndex(r => new { r.TenantId, r.ProductId, r.BranchId });
        builder.HasIndex(r => r.ProductId);

        builder.ComplexProperty(r => r.OverridePrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("OverridePriceAmount")
                .HasPrecision(18, 4)
                .IsRequired();
            money.Property(m => m.Currency)
                .HasColumnName("OverridePriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
