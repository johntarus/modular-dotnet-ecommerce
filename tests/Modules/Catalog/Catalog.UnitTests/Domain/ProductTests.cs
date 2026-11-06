using Catalog.Domain.Entities;
using Catalog.Domain.Events;
using ValidationException = BuildingBlocks.Exceptions.ValidationException;

namespace Catalog.UnitTests.Domain;

public class ProductTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static Product NewProduct(decimal price = 10m)
    {
        var product = Product.Create("Mug", "Ceramic mug", price, CategoryId);
        product.ClearDomainEvents();
        return product;
    }

    // ---------- Create ----------

    [Fact]
    public void Create_SetsProperties_AndRaisesProductCreated()
    {
        var product = Product.Create("Mug", "Ceramic mug", 12.50m, CategoryId);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Mug", product.Name);
        Assert.Equal("Ceramic mug", product.Description);
        Assert.Equal(12.50m, product.Price);
        Assert.Equal(CategoryId, product.CategoryId);
        Assert.False(product.IsDeleted);

        var created = Assert.IsType<ProductCreatedDomainEvent>(Assert.Single(product.DomainEvents));
        Assert.Equal(product.Id, created.ProductId);
        Assert.Equal("Mug", created.Name);
        Assert.Equal(12.50m, created.Price);
        Assert.Equal(CategoryId, created.CategoryId);
    }

    [Fact]
    public void Create_TrimsName()
    {
        Assert.Equal("Mug", Product.Create("  Mug \t", "d", 1m, CategoryId).Name);
    }

    [Fact]
    public void UpdateDetails_TrimsName()
    {
        var product = NewProduct();

        product.UpdateDetails(" Big mug ", "d", CategoryId);

        Assert.Equal("Big mug", product.Name);
    }

    [Fact]
    public void Create_AllowsEmptyDescription()
    {
        Assert.Equal("", Product.Create("Mug", "", 1m, CategoryId).Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_ThrowsValidation(string name)
    {
        Assert.Throws<ValidationException>(() => Product.Create(name, "d", 1m, CategoryId));
    }

    [Fact]
    public void Create_NullDescription_ThrowsValidation()
    {
        Assert.Throws<ValidationException>(() => Product.Create("Mug", null!, 1m, CategoryId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositivePrice_ThrowsValidation(decimal price)
    {
        Assert.Throws<ValidationException>(() => Product.Create("Mug", "d", price, CategoryId));
    }

    [Fact]
    public void Create_EmptyCategory_ThrowsValidation()
    {
        Assert.Throws<ValidationException>(() => Product.Create("Mug", "d", 1m, Guid.Empty));
    }

    // ---------- UpdateDetails ----------

    [Fact]
    public void UpdateDetails_ChangesFields_AndRaisesProductUpdated()
    {
        var product = NewProduct();
        var newCategory = Guid.NewGuid();

        product.UpdateDetails("Big mug", "Bigger", newCategory);

        Assert.Equal("Big mug", product.Name);
        Assert.Equal("Bigger", product.Description);
        Assert.Equal(newCategory, product.CategoryId);
        var updated = Assert.IsType<ProductUpdatedDomainEvent>(Assert.Single(product.DomainEvents));
        Assert.Equal(product.Id, updated.ProductId);
        Assert.Equal("Big mug", updated.Name);
    }

    [Fact]
    public void UpdateDetails_Invalid_ThrowsValidation_AndChangesNothing()
    {
        var product = NewProduct();

        Assert.Throws<ValidationException>(() => product.UpdateDetails(" ", "d", CategoryId));
        Assert.Throws<ValidationException>(() => product.UpdateDetails("Mug", null!, CategoryId));
        Assert.Throws<ValidationException>(() => product.UpdateDetails("Mug", "d", Guid.Empty));
        Assert.Equal("Mug", product.Name);
        Assert.Empty(product.DomainEvents);
    }

    // ---------- UpdatePrice ----------

    [Fact]
    public void UpdatePrice_Changed_RaisesPriceChangedWithOldAndNew()
    {
        var product = NewProduct(price: 10m);

        product.UpdatePrice(15m);

        Assert.Equal(15m, product.Price);
        var changed = Assert.IsType<ProductPriceChangedDomainEvent>(Assert.Single(product.DomainEvents));
        Assert.Equal(10m, changed.OldPrice);
        Assert.Equal(15m, changed.NewPrice);
    }

    [Fact]
    public void UpdatePrice_Unchanged_RaisesNoEvent()
    {
        var product = NewProduct(price: 10m);

        product.UpdatePrice(10m);

        Assert.Empty(product.DomainEvents);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void UpdatePrice_NonPositive_ThrowsValidation_AndKeepsPrice(decimal price)
    {
        var product = NewProduct(price: 10m);

        Assert.Throws<ValidationException>(() => product.UpdatePrice(price));
        Assert.Equal(10m, product.Price);
        Assert.Empty(product.DomainEvents);
    }

    // ---------- Delete ----------

    [Fact]
    public void Delete_MarksDeleted_AndRaisesProductDeleted()
    {
        var product = NewProduct();

        product.Delete();

        Assert.True(product.IsDeleted);
        Assert.Equal(product.Id, Assert.IsType<ProductDeletedDomainEvent>(Assert.Single(product.DomainEvents)).ProductId);
    }

    [Fact]
    public void Delete_Twice_RaisesOnlyOneEvent()
    {
        var product = NewProduct();

        product.Delete();
        product.Delete();

        Assert.Single(product.DomainEvents);
    }
}

public class CategoryTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var category = Category.Create("Kitchen", "Pots and pans");

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("Kitchen", category.Name);
        Assert.Equal("Pots and pans", category.Description);
        Assert.False(category.IsDeleted);
    }

    [Fact]
    public void Create_And_UpdateDetails_TrimName()
    {
        var category = Category.Create("  Kitchen  ", null);
        Assert.Equal("Kitchen", category.Name);

        category.UpdateDetails(" Home ", null);
        Assert.Equal("Home", category.Name);
    }

    [Fact]
    public void Create_AllowsNullDescription()
    {
        Assert.Null(Category.Create("Kitchen", null).Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_ThrowsValidation(string name)
    {
        Assert.Throws<ValidationException>(() => Category.Create(name, null));
    }

    [Fact]
    public void UpdateDetails_ChangesFields()
    {
        var category = Category.Create("Kitchen", null);

        category.UpdateDetails("Home", "Everything");

        Assert.Equal("Home", category.Name);
        Assert.Equal("Everything", category.Description);
    }

    [Fact]
    public void UpdateDetails_BlankName_ThrowsValidation_AndKeepsName()
    {
        var category = Category.Create("Kitchen", null);

        Assert.Throws<ValidationException>(() => category.UpdateDetails(" ", null));
        Assert.Equal("Kitchen", category.Name);
    }

    [Fact]
    public void Delete_MarksDeleted()
    {
        var category = Category.Create("Kitchen", null);

        category.Delete();

        Assert.True(category.IsDeleted);
    }
}
