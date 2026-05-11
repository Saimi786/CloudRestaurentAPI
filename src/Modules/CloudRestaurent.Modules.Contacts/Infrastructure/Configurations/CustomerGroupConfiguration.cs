using CloudRestaurent.Modules.Contacts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroup>
{
    public void Configure(EntityTypeBuilder<CustomerGroup> builder)
    {
        builder.ToTable("CustomerGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(1000);
        builder.Property(g => g.DiscountPercent).HasPrecision(5, 2);

        builder.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();
    }
}
