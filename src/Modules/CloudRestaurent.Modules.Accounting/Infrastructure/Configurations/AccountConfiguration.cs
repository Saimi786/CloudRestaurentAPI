using CloudRestaurent.Modules.Accounting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(500);
        builder.Property(a => a.Class).HasConversion<int>();

        builder.HasIndex(a => new { a.TenantId, a.Code }).IsUnique();
        builder.HasIndex(a => a.Class);
        builder.HasIndex(a => a.IsCashOrBank);
    }
}

public sealed class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.ToTable("AccountTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Side).HasConversion<int>();
        builder.Property(t => t.Amount).HasPrecision(18, 4);
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired();
        builder.Property(t => t.SourceType).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.BatchId).HasMaxLength(50);

        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => new { t.SourceType, t.SourceId });
        builder.HasIndex(t => t.BatchId);
        builder.HasIndex(t => t.OperationDate);
    }
}
