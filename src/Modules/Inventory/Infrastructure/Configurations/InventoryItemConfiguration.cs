using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");
        builder.HasKey(i => i.ProductId);
        builder.Property(i => i.QuantityOnHand).IsRequired();
        builder.Property(i => i.QuantityReserved).IsRequired();
        builder.HasQueryFilter(i => !i.IsDeleted);

    }
}