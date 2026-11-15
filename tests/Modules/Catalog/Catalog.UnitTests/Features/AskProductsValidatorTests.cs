using Catalog.Features.Products.Ask;
using FluentValidation.TestHelper;

namespace Catalog.UnitTests.Features;

public class AskProductsValidatorTests
{
    private readonly AskProductsValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ollama")]
    [InlineData("ollama")]
    [InlineData("OpenAI")]
    public void SupportedOrOmittedProvider_IsValid(string? provider)
    {
        _validator.TestValidate(new AskProductsQuery("headphones?", provider)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Gemini")]
    [InlineData("string")] // Swagger's placeholder value
    public void UnknownProvider_HasErrorListingSupportedProviders(string provider)
    {
        _validator.TestValidate(new AskProductsQuery("headphones?", provider))
            .ShouldHaveValidationErrorFor(x => x.providerName)
            .WithErrorMessage($"Unknown AI provider '{provider}'. Supported providers: Ollama, OpenAI.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void BlankQuestion_HasError(string question)
    {
        _validator.TestValidate(new AskProductsQuery(question)).ShouldHaveValidationErrorFor(x => x.Question);
    }
}
