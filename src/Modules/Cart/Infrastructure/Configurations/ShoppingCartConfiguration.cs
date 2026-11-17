using Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Infrastructure.Configurations;

internal sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        // 1. Table & Check Constraint
        builder.ToTable("Carts", t =>
            t.HasCheckConstraint(
                "CK_Cart_Identity",
                "(\"UserId\" IS NOT NULL AND \"GuestTokenHash\" IS NULL) OR (\"UserId\" IS NULL AND \"GuestTokenHash\" IS NOT NULL)"));

        builder.HasKey(c => c.Id);

        // 2. Partial Unique Indexes
        builder.HasIndex(c => c.UserId)
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");

        builder.HasIndex(c => c.GuestTokenHash)
            .IsUnique()
            .HasFilter("\"GuestTokenHash\" IS NOT NULL");

        // 3. Relationships & Properties
        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(c => c.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(c => c.Version)
            .IsRowVersion();
    }
}