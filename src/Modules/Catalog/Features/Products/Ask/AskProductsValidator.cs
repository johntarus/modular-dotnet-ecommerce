using Ai.Contracts;
using FluentValidation;

namespace Catalog.Features.Products.Ask;

internal sealed class AskProductsValidator : AbstractValidator<AskProductsQuery>
{
    public AskProductsValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty();

        // Rejected here so a bad provider costs nothing: the handler embeds the question before choosing one.
        RuleFor(x => x.providerName)
            .Must(name => LlmProviders.Normalize(name) is not null)
            .When(x => !string.IsNullOrWhiteSpace(x.providerName))
            .WithMessage(x => LlmProviders.UnknownProviderMessage(x.providerName));
    }
}
