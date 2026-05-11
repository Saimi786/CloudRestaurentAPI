using CloudRestaurent.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class ProductAvailabilityWindowConfiguration : IEntityTypeConfiguration<ProductAvailabilityWindow>
{
    public void Configure(EntityTypeBuilder<ProductAvailabilityWindow> builder)
    {
        builder.ToTable("ProductAvailabilityWindows");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).HasMaxLength(80).IsRequired();
        builder.Property(w => w.DaysOfWeek).HasConversion<int>();
        builder.HasIndex(w => w.ProductId);
        builder.HasIndex(w => w.IsActive);
    }
}
