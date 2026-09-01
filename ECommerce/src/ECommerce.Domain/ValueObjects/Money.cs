namespace ECommerce.Domain.ValueObjects;

public sealed record Money
{
    private const decimal MaximumAmount =
        9_999_999_999_999_999.99m;

    private Money()
    {
    }

    public Money(
        decimal amount,
        string currencyCode)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Money amount cannot be negative.");
        }

        if (amount > MaximumAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Money amount exceeds the supported maximum.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException(
                "Money amount cannot contain more than " +
                "two decimal places.",
                nameof(amount));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            currencyCode);

        var normalizedCurrencyCode =
            currencyCode.Trim().ToUpperInvariant();

        if (normalizedCurrencyCode.Length != 3 ||
            normalizedCurrencyCode.Any(character =>
                character < 'A' ||
                character > 'Z'))
        {
            throw new ArgumentException(
                "Currency code must contain exactly " +
                "three letters.",
                nameof(currencyCode));
        }

        Amount = amount;
        CurrencyCode = normalizedCurrencyCode;
    }

    public decimal Amount { get; private init; }

    public string CurrencyCode { get; private init; }
        = string.Empty;
}