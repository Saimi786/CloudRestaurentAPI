using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount).HasPrecision(18, 4);
        builder.Property(r => r.Currency).HasMaxLength(3).IsRequired();
        builder.Property(r => r.Method).HasConversion<int>();
        builder.Property(r => r.Reason).HasMaxLength(500);

        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => new { r.TenantId, r.RefundedAt });

        builder.HasMany(r => r.Lines)
            .WithOne()
            .HasForeignKey(l => l.RefundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefundLineConfiguration : IEntityTypeConfiguration<RefundLine>
{
    public void Configure(EntityTypeBuilder<RefundLine> builder)
    {
        builder.ToTable("RefundLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.HasIndex(l => l.RefundId);
    }
}
