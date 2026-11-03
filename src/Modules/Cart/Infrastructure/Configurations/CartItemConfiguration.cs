using Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Infrastructure.Configurations;

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items");
        builder.HasKey(i => i.Id);
        // Id is assigned in the domain (Guid.NewGuid()), so EF must not treat a
        // non-default key as "already exists" — otherwise new items get UPDATEd, not INSERTed.
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.HasIndex(i => new { i.CartId, i.ProductId }).IsUnique();
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
    }
}