namespace Orders.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException(
                "Amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency) ||
            currency.Trim().Length != 3)
            throw new ArgumentException(
                "Currency must be a valid three-letter code.",
                nameof(currency));

        return new Money(
            amount,
            currency.Trim().ToUpperInvariant());
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);

        return Create(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        return Create(Amount * quantity, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                "Cannot perform operations on different currencies.");
    }
}