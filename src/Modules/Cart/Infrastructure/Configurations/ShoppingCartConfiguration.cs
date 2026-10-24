using Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Infrastructure.Configurations;

internal sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.ToTable("shopping_carts");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c=>c.UserId).IsUnique();
        builder.HasMany(c=>c.Items)
            .WithOne().HasForeignKey(c=>c.CartId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(c => c.Version).IsRowVersion();
    }
}