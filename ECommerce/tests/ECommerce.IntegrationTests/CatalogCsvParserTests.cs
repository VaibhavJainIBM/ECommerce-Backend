using System.Globalization;
using System.Text;
using ECommerce.Infrastructure.Files;

namespace ECommerce.IntegrationTests;

public sealed class CatalogCsvParserTests
{
    private const string Header =
        "ProductKey,Title,BrandName,Description,VariantName,VariantCode,Gtin";

    [Fact]
    public async Task QuotedCommaCsv_ProducesExpectedRow()
    {
        const string csv =
            Header + "\r\n" +
            "PHONE-1,\"Phone, Pro\",Fabrikam,\"Fast phone, imported in bulk\",Black / 128 GB,PHONE-BLK-128,12345678\r\n";
        await using var stream = CsvStream(csv);

        var result = await new CatalogCsvParser()
            .ParseAsync(stream);

        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Rows);
        Assert.Equal(2, row.RowNumber);
        Assert.Equal("Phone, Pro", row.Title);
        Assert.Equal(
            "Fast phone, imported in bulk",
            row.Description);
        Assert.Equal("PHONE-BLK-128", row.VariantCode);
    }

    [Fact]
    public async Task UploadReadyHundredSkuCsv_ProducesExactlyHundredRows()
    {
        var csv = BuildHundredSkuCsv();
        await using var stream = CsvStream(csv);

        var result = await new CatalogCsvParser()
            .ParseAsync(stream);

        Assert.Empty(result.Errors);
        Assert.Equal(100, result.Rows.Count);
        Assert.Equal(10, result.Rows
            .Select(row => row.ProductKey)
            .Distinct(StringComparer.Ordinal)
            .Count());
        Assert.Equal(100, result.Rows
            .Select(row => row.VariantCode)
            .Distinct(StringComparer.Ordinal)
            .Count());
        Assert.Equal(2, result.Rows.First().RowNumber);
        Assert.Equal(101, result.Rows.Last().RowNumber);
    }

    private static string BuildHundredSkuCsv()
    {
        var csv = new StringBuilder()
            .AppendLine(Header);

        for (var product = 1; product <= 10; product++)
        {
            for (var variant = 1; variant <= 10; variant++)
            {
                var sequence = (product - 1) * 10 + variant;
                var gtin = (8_900_000_000_000L + sequence)
                    .ToString(CultureInfo.InvariantCulture);

                csv.Append("PRODUCT-")
                    .Append(product.ToString("D3", CultureInfo.InvariantCulture))
                    .Append(",Demo Product ")
                    .Append(product.ToString(CultureInfo.InvariantCulture))
                    .Append(",Fabrikam,Bulk import sample,Option ")
                    .Append(variant.ToString(CultureInfo.InvariantCulture))
                    .Append(",PRODUCT-")
                    .Append(product.ToString("D3", CultureInfo.InvariantCulture))
                    .Append("-SKU-")
                    .Append(variant.ToString("D3", CultureInfo.InvariantCulture))
                    .Append(',')
                    .AppendLine(gtin);
            }
        }

        return csv.ToString();
    }

    private static MemoryStream CsvStream(string value) =>
        new(Encoding.UTF8.GetBytes(value));
}
