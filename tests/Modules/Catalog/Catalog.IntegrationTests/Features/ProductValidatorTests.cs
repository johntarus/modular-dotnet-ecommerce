using Catalog.Features.Products.CreateProduct;
using Catalog.Features.Products.UpdateProduct;
using Catalog.IntegrationTests.Infrastructure;
using FluentValidation.TestHelper;

namespace Catalog.IntegrationTests.Features;

/// <summary>The product validators check the category exists, so they run against the database.</summary>
public class CreateProductValidatorTests(PostgresFixture db) : CatalogTestBase(db)
{
    private async Task<TestValidationResult<CreateProductCommand>> ValidateAsync(CreateProductCommand command)
    {
        await using var context = NewContext();
        return await new CreateProductValidator(context).TestValidateAsync(command);
    }

    [Fact]
    public async Task Valid_HasNoErrors()
    {
        var categoryId = await SeedCategoryAsync();

        (await ValidateAsync(new CreateProductCommand("Mug", "d", 1m, categoryId))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UnknownCategory_HasError()
    {
        (await ValidateAsync(new CreateProductCommand("Mug", "d", 1m, Guid.NewGuid())))
            .ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task DeletedCategory_HasError()
    {
        var deleted = await SeedCategoryAsync(deleted: true);

        (await ValidateAsync(new CreateProductCommand("Mug", "d", 1m, deleted)))
            .ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task EmptyCategory_ReportsOneError_NotTwo()
    {
        var result = await ValidateAsync(new CreateProductCommand("Mug", "d", 1m, Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
        Assert.Single(result.Errors, e => e.ErrorMessage.Contains("must not be empty"));
    }

    [Fact]
    public async Task MissingOrOverlongDescription_HasError()
    {
        var categoryId = await SeedCategoryAsync();

        (await ValidateAsync(new CreateProductCommand("Mug", null!, 1m, categoryId))).ShouldHaveValidationErrorFor(x => x.Description);
        (await ValidateAsync(new CreateProductCommand("Mug", new string('d', 1001), 1m, categoryId))).ShouldHaveValidationErrorFor(x => x.Description);
        (await ValidateAsync(new CreateProductCommand("Mug", "", 1m, categoryId))).ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("Mug", 0)]
    [InlineData("Mug", -1)]
    public async Task BlankNameOrNonPositivePrice_HasErrors(string name, decimal price)
    {
        var categoryId = await SeedCategoryAsync();

        Assert.False((await ValidateAsync(new CreateProductCommand(name, "d", price, categoryId))).IsValid);
    }

    [Theory]
    [InlineData("Mug")]
    [InlineData("MUG")]
    [InlineData("Mug ")]
    [InlineData("  mug")]
    public async Task DuplicateNameInSameCategory_IgnoringCaseAndSurroundingSpaces_HasError(string name)
    {
        var categoryId = await SeedCategoryAsync();
        await SeedProductAsync(categoryId, name: "Mug");

        (await ValidateAsync(new CreateProductCommand(name, "d", 1m, categoryId))).ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("A product with this name already exists in this category.");
    }

    [Fact]
    public async Task SameNameInDifferentCategory_IsValid()
    {
        await SeedProductAsync(await SeedCategoryAsync("Kitchen"), name: "Mug");
        var garden = await SeedCategoryAsync("Garden");

        (await ValidateAsync(new CreateProductCommand("Mug", "d", 1m, garden))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task NameOfDeletedProduct_CanBeReused()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedProductAsync(categoryId, name: "Mug", deleted: true);

        (await ValidateAsync(new CreateProductCommand("Mug", "d", 1m, categoryId))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task NameOverLimit_HasError()
    {
        var categoryId = await SeedCategoryAsync();

        (await ValidateAsync(new CreateProductCommand(new string('n', 201), "d", 1m, categoryId))).ShouldHaveValidationErrorFor(x => x.Name);
    }
}

public class UpdateProductValidatorTests(PostgresFixture db) : CatalogTestBase(db)
{
    private async Task<TestValidationResult<UpdateProductCommand>> ValidateAsync(UpdateProductCommand command)
    {
        await using var context = NewContext();
        return await new UpdateProductValidator(context).TestValidateAsync(command);
    }

    [Fact]
    public async Task Valid_HasNoErrors()
    {
        var categoryId = await SeedCategoryAsync();

        (await ValidateAsync(new UpdateProductCommand("Mug", "d", 1m, categoryId))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UnknownCategory_HasError()
    {
        (await ValidateAsync(new UpdateProductCommand("Mug", "d", 1m, Guid.NewGuid()))).ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public async Task KeepingOwnName_IsValid()
    {
        var categoryId = await SeedCategoryAsync();
        var id = await SeedProductAsync(categoryId, name: "Mug");

        (await ValidateAsync(new UpdateProductCommand("mug", "d", 2m, categoryId) { Id = id })).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task RenamingToSiblingsName_HasError()
    {
        var categoryId = await SeedCategoryAsync();
        await SeedProductAsync(categoryId, name: "Mug");
        var bowl = await SeedProductAsync(categoryId, name: "Bowl");

        (await ValidateAsync(new UpdateProductCommand("Mug", "d", 1m, categoryId) { Id = bowl })).ShouldHaveValidationErrorFor(x => x.Name);
        (await ValidateAsync(new UpdateProductCommand(" Mug ", "d", 1m, categoryId) { Id = bowl })).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task MovingIntoCategoryThatHasSameName_HasError()
    {
        var kitchen = await SeedCategoryAsync("Kitchen");
        var garden = await SeedCategoryAsync("Garden");
        await SeedProductAsync(garden, name: "Mug");
        var id = await SeedProductAsync(kitchen, name: "Mug");

        (await ValidateAsync(new UpdateProductCommand("Mug", "d", 1m, garden) { Id = id })).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task MissingOrOverlongDescription_HasError()
    {
        var categoryId = await SeedCategoryAsync();

        (await ValidateAsync(new UpdateProductCommand("Mug", null!, 1m, categoryId))).ShouldHaveValidationErrorFor(x => x.Description);
        (await ValidateAsync(new UpdateProductCommand("Mug", new string('d', 1001), 1m, categoryId))).ShouldHaveValidationErrorFor(x => x.Description);
    }
}
