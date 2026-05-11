using CloudRestaurent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.ToTable("UserBranches");
        builder.HasKey(ub => ub.Id);
        builder.HasIndex(ub => new { ub.UserId, ub.BranchId }).IsUnique();
        builder.HasIndex(ub => ub.BranchId);
        builder.HasIndex(ub => ub.TenantId);
    }
}
