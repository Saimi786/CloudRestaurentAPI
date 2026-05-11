using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class OrderPromotionConfiguration : IEntityTypeConfiguration<OrderPromotion>
{
    public void Configure(EntityTypeBuilder<OrderPromotion> builder)
    {
        builder.ToTable("OrderPromotions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(300);
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.HasIndex(p => p.OrderId);
    }
}
