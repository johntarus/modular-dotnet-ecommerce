using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", "orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.ConfirmedAt);

        builder.Property(x => x.CancelledAt);

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(500);

        // Shipping cost value object
        builder.OwnsOne(x => x.ShippingCost, money =>
        {
            money.Property(x => x.Amount)
                .HasColumnName("shipping_cost_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(x => x.Currency)
                .HasColumnName("shipping_cost_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Delivery address value object
        builder.OwnsOne(x => x.DeliveryAddress, address =>
        {
            address.Property(x => x.RecipientName)
                .HasColumnName("recipient_name")
                .HasMaxLength(150)
                .IsRequired();

            address.Property(x => x.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(30)
                .IsRequired();

            address.Property(x => x.AddressLine)
                .HasColumnName("address_line")
                .HasMaxLength(250)
                .IsRequired();

            address.Property(x => x.City)
                .HasColumnName("city")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(x => x.County)
                .HasColumnName("county")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(x => x.Country)
                .HasColumnName("country")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(x => x.PostalCode)
                .HasColumnName("postal_code")
                .HasMaxLength(20)
                .IsRequired();
        });

        // Order items owned collection
        builder.OwnsMany(x => x.Items, item =>
        {
            item.ToTable("order_items", "orders");

            item.WithOwner()
                .HasForeignKey("OrderId");

            item.HasKey(x => x.Id);

            item.Property(x => x.Id)
                .ValueGeneratedNever();

            item.Property(x => x.ProductId)
                .IsRequired();

            item.Property(x => x.ProductName)
                .HasMaxLength(200)
                .IsRequired();

            item.Property(x => x.Sku)
                .HasMaxLength(100)
                .IsRequired();

            item.Property(x => x.Quantity)
                .IsRequired();

            // Unit price value object
            item.OwnsOne(x => x.UnitPrice, money =>
            {
                money.Property(x => x.Amount)
                    .HasColumnName("unit_price_amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(x => x.Currency)
                    .HasColumnName("unit_price_currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            item.Ignore(x => x.LineTotal);

            item.HasIndex(x => x.ProductId);
        });

        builder.Navigation(x => x.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Computed property: calculated in the domain
        builder.Ignore(x => x.TotalAmount);

        // Indexes
        builder.HasIndex(x => x.CustomerId);

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.CreatedAt);
    }
}