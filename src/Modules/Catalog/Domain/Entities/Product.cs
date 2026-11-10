using BuildingBlocks.Domain;
using BuildingBlocks.Exceptions;
using Catalog.Domain.Events;
using Pgvector;

namespace Catalog.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsDeleted { get; private set; }
    public Vector? Embedding { get; private set; }
    public DateTime? EmbeddingGeneratedAt { get; private set; }

    private Product()
    {
    }

    public static Product Create(string name, string description, decimal price, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Product Name cannot be empty.");
        if (description is null)
            throw new ValidationException("Product Description is required.");
        if (price <= 0)
            throw new ValidationException("Product Price must be greater than zero.");
        if (categoryId == Guid.Empty)
            throw new ValidationException("Product Category must be provided.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
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
            throw new ValidationException("Name cannot be empty.");
        if (description is null)
            throw new ValidationException("Description is required.");
        if (categoryId == Guid.Empty)
            throw new ValidationException("Category cannot be empty.");

        var trimmedName = name.Trim();

        var textChanged =
            !string.Equals(Name, trimmedName, StringComparison.Ordinal) ||
            !string.Equals(Description, description, StringComparison.Ordinal);

        Name = trimmedName;
        Description = description;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;

        if (textChanged)
        {
            Embedding = null;
            EmbeddingGeneratedAt = null;
        }

        AddDomainEvent(new ProductUpdatedDomainEvent(Id, Name, Description, CategoryId));
    }
    
    
    public void UpdatePrice(decimal price)
    {
        if (price <= 0)
            throw new ValidationException("Price must be greater than zero.");

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
    
    public void SetEmbedding(Vector embedding)
    {
        ArgumentNullException.ThrowIfNull(embedding);

        if (embedding.ToArray().Length != 1024)
            throw new ArgumentException(
                $"Embedding must be 1024 dimensions, got {embedding.ToArray().Length}.",
                nameof(embedding));

        Embedding = embedding;
        EmbeddingGeneratedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductEmbeddingGeneratedDomainEvent(Id, EmbeddingGeneratedAt.Value));
    }
    public string GetEmbeddingText()
        => $"{Name}. {Description}".Trim();
}