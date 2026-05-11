using CloudRestaurent.Modules.Accounting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reference).HasMaxLength(60).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Amount).HasPrecision(18, 4);
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.Method).HasConversion<int>();

        builder.HasIndex(e => new { e.TenantId, e.OccurredAt });
        builder.HasIndex(e => e.BranchId);
        builder.HasIndex(e => e.ExpenseAccountId);
    }
}
