using Catalog.Features.Categories.CreateCategory;
using Catalog.Features.Categories.UpdateCategory;
using Catalog.IntegrationTests.Infrastructure;
using FluentValidation.TestHelper;

namespace Catalog.IntegrationTests.Features;

/// <summary>The category validators check name uniqueness, so they run against the database.</summary>
public class CreateCategoryValidatorTests(PostgresFixture db) : CatalogTestBase(db)
{
    private async Task<TestValidationResult<CreateCategoryCommand>> ValidateAsync(string name, string? description = null)
    {
        await using var context = NewContext();
        return await new CreateCategoryValidator(context).TestValidateAsync(new CreateCategoryCommand(name, description));
    }

    [Fact]
    public async Task Valid_HasNoErrors()
    {
        (await ValidateAsync("Kitchen")).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task MissingName_HasError(string? name)
    {
        (await ValidateAsync(name!)).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task NameAtLimit_IsValid_AndOverLimit_HasError()
    {
        (await ValidateAsync(new string('a', 200))).ShouldNotHaveAnyValidationErrors();
        (await ValidateAsync(new string('a', 201))).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task DescriptionOverLimit_HasError()
    {
        (await ValidateAsync("Kitchen", new string('a', 501))).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData("Kitchen")]
    [InlineData("KITCHEN")]
    [InlineData("kitchen")]
    [InlineData("Kitchen ")]
    [InlineData("  kitchen\t")]
    public async Task DuplicateName_IgnoringCaseAndSurroundingSpaces_HasError(string name)
    {
        await SeedCategoryAsync("Kitchen");

        (await ValidateAsync(name)).ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("A category with this name already exists.");
    }

    [Fact]
    public async Task NameOfDeletedCategory_CanBeReused()
    {
        await SeedCategoryAsync("Kitchen", deleted: true);

        (await ValidateAsync("Kitchen")).ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateCategoryValidatorTests(PostgresFixture db) : CatalogTestBase(db)
{
    private async Task<TestValidationResult<UpdateCategoryCommand>> ValidateAsync(UpdateCategoryCommand command)
    {
        await using var context = NewContext();
        return await new UpdateCategoryValidator(context).TestValidateAsync(command);
    }

    [Fact]
    public async Task Valid_HasNoErrors()
    {
        (await ValidateAsync(new UpdateCategoryCommand(Guid.NewGuid(), "Kitchen", "d"))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task EmptyId_HasError()
    {
        (await ValidateAsync(new UpdateCategoryCommand(Guid.Empty, "Kitchen", null))).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public async Task BlankName_HasError()
    {
        (await ValidateAsync(new UpdateCategoryCommand(Guid.NewGuid(), "", null))).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Overlong_HasErrors()
    {
        var result = await ValidateAsync(new UpdateCategoryCommand(Guid.NewGuid(), new string('a', 201), new string('a', 501)));

        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task KeepingOwnName_IsValid_EvenWithDifferentCase()
    {
        var id = await SeedCategoryAsync("Kitchen");

        (await ValidateAsync(new UpdateCategoryCommand(id, "KITCHEN", null))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task KeepingOwnName_WithExtraSpaces_IsValid()
    {
        var id = await SeedCategoryAsync("Kitchen");

        (await ValidateAsync(new UpdateCategoryCommand(id, " Kitchen ", null))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task RenamingToAnotherCategorysName_HasError()
    {
        await SeedCategoryAsync("Kitchen");
        var garden = await SeedCategoryAsync("Garden");

        (await ValidateAsync(new UpdateCategoryCommand(garden, "kitchen", null))).ShouldHaveValidationErrorFor(x => x.Name);
        (await ValidateAsync(new UpdateCategoryCommand(garden, " Kitchen ", null))).ShouldHaveValidationErrorFor(x => x.Name);
    }
}
