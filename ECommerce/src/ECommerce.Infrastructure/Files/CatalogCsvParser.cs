using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ECommerce.Application.Abstractions.Files;
using ECommerce.Application.Catalog.Importing;
using ECommerce.Application.Common;

namespace ECommerce.Infrastructure.Files;

public sealed class CatalogCsvParser : ICatalogCsvParser
{
    private const int MaximumDataRows = 1000;

    private static readonly string[] RequiredHeaders =
    [
        "ProductKey",
        "Title",
        "BrandName",
        "Description",
        "VariantName",
        "VariantCode",
        "Gtin"
    ];

    public async Task<CatalogCsvParseResult> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var configuration =
            new CsvConfiguration(
                CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                ExceptionMessagesContainRawData = false
            };

        try
        {
            using var textReader = new StreamReader(
                stream,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true),
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 4096,
                leaveOpen: true);
            using var csv = new CsvReader(
                textReader,
                configuration);

            cancellationToken.ThrowIfCancellationRequested();

            if (!await csv.ReadAsync())
            {
                return Failure(
                    CatalogImportErrors.HeaderRequired);
            }

            csv.ReadHeader();

            var headerErrors = ValidateHeaders(
                csv.HeaderRecord);

            if (headerErrors.Count > 0)
            {
                return new CatalogCsvParseResult(
                    Array.Empty<CatalogCsvRow>(),
                    headerErrors);
            }

            var rows = new List<CatalogCsvRow>();

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var values = RequiredHeaders
                    .Select(header =>
                        csv.GetField(header))
                    .ToArray();

                if (values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (rows.Count == MaximumDataRows)
                {
                    return Failure(
                        CatalogImportErrors.TooManyRows);
                }

                rows.Add(
                    new CatalogCsvRow(
                        checked((int)csv.Parser.Row),
                        values[0],
                        values[1],
                        values[2],
                        values[3],
                        values[4],
                        values[5],
                        values[6]));
            }

            return new CatalogCsvParseResult(
                rows,
                Array.Empty<Error>());
        }
        catch (DecoderFallbackException)
        {
            return Failure(
                CatalogImportErrors.InvalidCsv);
        }
        catch (CsvHelperException)
        {
            return Failure(
                CatalogImportErrors.InvalidCsv);
        }
        catch (InvalidDataException)
        {
            return Failure(
                CatalogImportErrors.InvalidCsv);
        }
    }

    private static IReadOnlyCollection<Error>
        ValidateHeaders(string[]? headers)
    {
        if (headers is null || headers.Length == 0)
        {
            return [CatalogImportErrors.HeaderRequired];
        }

        var errors = new List<Error>();
        var seen = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var header in headers)
        {
            var normalized = header.Trim();

            if (!seen.Add(normalized))
            {
                errors.Add(
                    CatalogImportErrors.DuplicateHeader(
                        normalized));
            }

            if (!RequiredHeaders.Contains(
                    normalized,
                    StringComparer.Ordinal))
            {
                errors.Add(
                    CatalogImportErrors.UnexpectedHeader(
                        normalized));
            }
        }

        foreach (var required in RequiredHeaders)
        {
            if (!seen.Contains(required))
            {
                errors.Add(
                    CatalogImportErrors.MissingHeader(
                        required));
            }
        }

        return errors;
    }

    private static CatalogCsvParseResult Failure(
        Error error)
    {
        return new CatalogCsvParseResult(
            Array.Empty<CatalogCsvRow>(),
            [error]);
    }
}
