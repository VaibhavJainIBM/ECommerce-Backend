namespace ECommerce.Domain.ValueObjects;

public sealed record Address
{
    private Address()
    {
    }

    public Address(
        string line1,
        string city,
        string stateOrProvince,
        string postalCode,
        string countryCode,
        string? line2 = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateOrProvince);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        var normalizedCountryCode =
            countryCode.Trim().ToUpperInvariant();

        if (normalizedCountryCode.Length != 2)
        {
            throw new ArgumentException(
                "Country code must contain exactly two characters.",
                nameof(countryCode));
        }

        Line1 = line1.Trim();
        Line2 = string.IsNullOrWhiteSpace(line2)
            ? null
            : line2.Trim();

        City = city.Trim();
        StateOrProvince = stateOrProvince.Trim();
        PostalCode = postalCode.Trim();
        CountryCode = normalizedCountryCode;
    }

    public string Line1 { get; private init; } = string.Empty;

    public string? Line2 { get; private init; }

    public string City { get; private init; } = string.Empty;

    public string StateOrProvince { get; private init; } = string.Empty;

    public string PostalCode { get; private init; } = string.Empty;

    public string CountryCode { get; private init; } = string.Empty;
}
