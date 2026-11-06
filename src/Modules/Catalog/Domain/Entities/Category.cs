using BuildingBlocks.Domain;
using BuildingBlocks.Exceptions;

namespace Catalog.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; }
    
    private Category()
    {
    }
    
    public static Category Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Category Name cannot be empty.");
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Category Name cannot be empty.");
        Name = name.Trim();
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Delete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}