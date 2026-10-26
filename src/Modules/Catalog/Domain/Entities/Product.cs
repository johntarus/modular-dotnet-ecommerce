using BuildingBlocks.Domain;
using BuildingBlocks.Shared;
using Catalog.Domain.Events;

namespace Catalog.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public Guid CategoryId { get; private set; }

    public bool IsDeleted { get; private set; }

    private Product()
    {
    }

    public static Product Create(string name, string description, decimal price, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product Name cannot be empty.", nameof(name));
        if (price <= 0)
            throw new ArgumentException("Product Price must be greater than zero.", nameof(price));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Product Category must be provided.", nameof(categoryId));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        product.AddDomainEvent(new ProductCreatedDomainEvent(
            product.Id,
            product.Name,
            product.Price,
            product.CategoryId));

        return product;
    }

    public void UpdateDetails(string name, string description, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if(categoryId == Guid.Empty)
            throw new ArgumentException("Category cannot be empty.", nameof(categoryId));

        Name = name;
        Description = description;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedDomainEvent(
            Id,
            Name,
            Description,
            CategoryId));
    }
    
    
    public void UpdatePrice(decimal price)
    {
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.");

        var oldPrice = Price;
        Price = price;
        UpdatedAt = DateTime.UtcNow;

        if (oldPrice != price)
        {
            AddDomainEvent(new ProductPriceChangedDomainEvent(
                Id,
                oldPrice,
                price));
        }
    }
    public void Delete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductDeletedDomainEvent(Id));
    }
}