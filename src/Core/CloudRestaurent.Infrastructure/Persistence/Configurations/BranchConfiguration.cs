using CloudRestaurent.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Code).HasMaxLength(50).IsRequired();
        builder.Property(b => b.PhoneNumber).HasMaxLength(50);
        builder.Property(b => b.ReceiptTemplate).HasConversion<int>();
        builder.Property(b => b.ReceiptFooterText).HasMaxLength(500);

        builder.HasIndex(b => new { b.TenantId, b.CompanyId, b.Code }).IsUnique();

        builder.OwnsOne(b => b.Location, loc =>
        {
            loc.Property(l => l.AddressLine1).HasMaxLength(300);
            loc.Property(l => l.AddressLine2).HasMaxLength(300);
            loc.Property(l => l.City).HasMaxLength(100);
            loc.Property(l => l.State).HasMaxLength(100);
            loc.Property(l => l.Country).HasMaxLength(100);
            loc.Property(l => l.PostalCode).HasMaxLength(20);
            loc.Property(l => l.TimeZone).HasMaxLength(50).IsRequired();
        });
    }
}
