using CloudRestaurent.Modules.Contacts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.SupplierBusinessName).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.TaxNumber).HasMaxLength(50);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.Type).HasConversion<int>();
        builder.Property(c => c.Gender).HasConversion<int?>();

        // Phone is unique per tenant, but only when it's actually set.
        // Filtered unique index — multiple null/empty phones allowed.
        builder.HasIndex(c => new { c.TenantId, c.Phone })
            .IsUnique()
            .HasFilter("[Phone] IS NOT NULL");

        builder.HasIndex(c => new { c.TenantId, c.FullName });
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.CustomerGroupId);

        builder.ComplexProperty(c => c.OpeningBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("OpeningBalanceAmount")
                .HasPrecision(18, 4)
                .IsRequired();
            money.Property(m => m.Currency)
                .HasColumnName("OpeningBalanceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.ComplexProperty(c => c.CurrentBalance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CurrentBalanceAmount")
                .HasPrecision(18, 4)
                .IsRequired();
            money.Property(m => m.Currency)
                .HasColumnName("CurrentBalanceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // CreditLimit is optional — store as discrete columns and ignore the Money? property.
        builder.Property<decimal?>("CreditLimitAmount").HasPrecision(18, 4);
        builder.Property<string?>("CreditLimitCurrency").HasMaxLength(3);
        builder.Ignore(c => c.CreditLimit);

        builder.ComplexProperty(c => c.BillingAddress, addr =>
        {
            addr.Property(a => a.Line1).HasColumnName("BillingLine1").HasMaxLength(300);
            addr.Property(a => a.Line2).HasColumnName("BillingLine2").HasMaxLength(300);
            addr.Property(a => a.City).HasColumnName("BillingCity").HasMaxLength(100);
            addr.Property(a => a.State).HasColumnName("BillingState").HasMaxLength(100);
            addr.Property(a => a.Country).HasColumnName("BillingCountry").HasMaxLength(100);
            addr.Property(a => a.PostalCode).HasColumnName("BillingPostalCode").HasMaxLength(20);
        });

        builder.ComplexProperty(c => c.ShippingAddress, addr =>
        {
            addr.Property(a => a.Line1).HasColumnName("ShippingLine1").HasMaxLength(300);
            addr.Property(a => a.Line2).HasColumnName("ShippingLine2").HasMaxLength(300);
            addr.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100);
            addr.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(100);
            addr.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(100);
            addr.Property(a => a.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20);
        });
    }
}
