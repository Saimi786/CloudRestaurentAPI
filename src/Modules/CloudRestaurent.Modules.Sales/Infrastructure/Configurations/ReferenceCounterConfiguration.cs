using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class ReferenceCounterConfiguration : IEntityTypeConfiguration<ReferenceCounter>
{
    public void Configure(EntityTypeBuilder<ReferenceCounter> builder)
    {
        builder.ToTable("ReferenceCounters");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DocumentType).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Prefix).HasMaxLength(20).IsRequired();

        // Each tenant+branch+document-type combination has exactly one counter row.
        builder.HasIndex(c => new { c.TenantId, c.BranchId, c.DocumentType }).IsUnique();
    }
}
