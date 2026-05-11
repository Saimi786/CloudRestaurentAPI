using CloudRestaurent.Modules.Restaurant.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class KitchenTicketConfiguration : IEntityTypeConfiguration<KitchenTicket>
{
    public void Configure(EntityTypeBuilder<KitchenTicket> builder)
    {
        builder.ToTable("KitchenTickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Status).HasConversion<int>();
        builder.Property(t => t.BumpedStationsRaw).HasMaxLength(2000);

        builder.HasIndex(t => new { t.TenantId, t.BranchId, t.Status });
        builder.HasIndex(t => t.OrderId).IsUnique();   // one ticket per order in v1
        builder.HasIndex(t => t.OpenedAt);
    }
}
