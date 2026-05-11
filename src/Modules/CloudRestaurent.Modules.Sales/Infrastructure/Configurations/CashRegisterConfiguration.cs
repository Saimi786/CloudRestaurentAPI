using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("CashRegisters");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(r => new { r.TenantId, r.BranchId, r.Code }).IsUnique();
        builder.HasIndex(r => r.IsActive);
    }
}

public sealed class CashRegisterShiftConfiguration : IEntityTypeConfiguration<CashRegisterShift>
{
    public void Configure(EntityTypeBuilder<CashRegisterShift> builder)
    {
        builder.ToTable("CashRegisterShifts");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OpeningAmount).HasPrecision(18, 4);
        builder.Property(s => s.DeclaredClosingAmount).HasPrecision(18, 4);
        builder.Property(s => s.ExpectedClosingAmount).HasPrecision(18, 4);
        builder.Property(s => s.OverShortAmount).HasPrecision(18, 4);
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(1000);
        builder.Property(s => s.Status).HasConversion<int>();

        builder.HasIndex(s => new { s.CashRegisterId, s.Status });
        builder.HasIndex(s => new { s.TenantId, s.OpenedByUserId, s.Status });
        builder.HasIndex(s => s.OpenedAt);

        builder.HasMany(s => s.Movements)
            .WithOne()
            .HasForeignKey(m => m.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CashRegisterShiftMovementConfiguration : IEntityTypeConfiguration<CashRegisterShiftMovement>
{
    public void Configure(EntityTypeBuilder<CashRegisterShiftMovement> builder)
    {
        builder.ToTable("CashRegisterShiftMovements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Amount).HasPrecision(18, 4);
        builder.Property(m => m.Type).HasConversion<int>();
        builder.Property(m => m.Reference).HasMaxLength(120);
        builder.Property(m => m.Notes).HasMaxLength(500);
        builder.HasIndex(m => m.ShiftId);
        builder.HasIndex(m => new { m.ShiftId, m.Type });
    }
}
