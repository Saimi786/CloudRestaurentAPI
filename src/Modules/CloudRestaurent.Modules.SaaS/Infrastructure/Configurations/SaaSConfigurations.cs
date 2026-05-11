using CloudRestaurent.Modules.SaaS.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("Packages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
        builder.Property(p => p.Plan).HasConversion<int>();
        builder.Property(p => p.Interval).HasConversion<int>();
        builder.Property(p => p.Price).HasPrecision(18, 4);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.FeatureNotes).HasMaxLength(2000);
        builder.HasIndex(p => p.Code).IsUnique();
    }
}

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Status).HasConversion<int>();
        builder.Property(s => s.AppliedDiscountPercent).HasPrecision(5, 2);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.TenantId, s.Status });
    }
}

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.DiscountPercent).HasPrecision(5, 2);
        builder.HasIndex(c => c.Code).IsUnique();
    }
}
