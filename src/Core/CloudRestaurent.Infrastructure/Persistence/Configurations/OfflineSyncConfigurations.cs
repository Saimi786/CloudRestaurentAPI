using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Key).HasMaxLength(120).IsRequired();
        builder.Property(r => r.Method).HasMaxLength(10).IsRequired();
        builder.Property(r => r.Path).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ContentType).HasMaxLength(100);
        // Scoped per (Key, UserId) so two devices can't read each other's cached responses.
        builder.HasIndex(r => new { r.Key, r.UserId }).IsUnique();
        builder.HasIndex(r => r.ExpiresAt);
    }
}

public sealed class SyncOperationConfiguration : IEntityTypeConfiguration<SyncOperation>
{
    public void Configure(EntityTypeBuilder<SyncOperation> builder)
    {
        builder.ToTable("SyncOperations");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Method).HasMaxLength(10).IsRequired();
        builder.Property(s => s.Path).HasMaxLength(500).IsRequired();
        builder.Property(s => s.ClientId).HasMaxLength(100);
        builder.Property(s => s.IdempotencyKey).HasMaxLength(120);
        builder.Property(s => s.Source).HasConversion<int>();
        builder.HasIndex(s => s.OccurredAt);
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.IdempotencyKey);
    }
}

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(a => a.EntityKey).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Kind).HasConversion<int>();
        builder.Property(a => a.RequestPath).HasMaxLength(500);
        builder.Property(a => a.IdempotencyKey).HasMaxLength(120);
        // BeforeJson/AfterJson can be sizeable on bulk Modified rows — let SQL Server pick nvarchar(max).
        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.EntityType, a.EntityKey });
    }
}
