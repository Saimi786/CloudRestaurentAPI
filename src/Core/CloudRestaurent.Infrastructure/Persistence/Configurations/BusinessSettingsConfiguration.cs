using CloudRestaurent.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class BusinessSettingsConfiguration : IEntityTypeConfiguration<BusinessSettings>
{
    public void Configure(EntityTypeBuilder<BusinessSettings> builder)
    {
        builder.ToTable("BusinessSettings");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.TenantId).IsUnique();

        builder.Property(s => s.DefaultCurrency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.DefaultTimezone).HasMaxLength(64).IsRequired();
        builder.Property(s => s.TaxLabel).HasMaxLength(32).IsRequired();
        builder.Property(s => s.RewardPointsName).HasMaxLength(32).IsRequired();
        builder.Property(s => s.SalesPrefix).HasMaxLength(8).IsRequired();
        builder.Property(s => s.PurchasePrefix).HasMaxLength(8).IsRequired();
        builder.Property(s => s.ExpensePrefix).HasMaxLength(8).IsRequired();
        builder.Property(s => s.CustomerPrefix).HasMaxLength(8).IsRequired();

        builder.Property(s => s.RewardPointsAmountPerPoint).HasPrecision(18, 4);
        builder.Property(s => s.RewardPointsMinOrderForEarn).HasPrecision(18, 2);
        builder.Property(s => s.RewardPointsRedeemValue).HasPrecision(18, 6);
        builder.Property(s => s.RewardPointsMinOrderForRedeem).HasPrecision(18, 2);
        builder.Property(s => s.RewardPointsExpiryUnit).HasConversion<int>();
    }
}
