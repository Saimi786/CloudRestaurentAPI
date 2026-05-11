using CloudRestaurent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.Permission).HasMaxLength(100).IsRequired();
        builder.HasIndex(rp => new { rp.RoleId, rp.Permission }).IsUnique();
        builder.HasIndex(rp => rp.Permission);
    }
}
