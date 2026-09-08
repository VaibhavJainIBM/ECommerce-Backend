using System;
using System.Collections.Generic;
using System.Text;

using ECommerce.Application.Common;
using ECommerce.Application.Catalog.Importing;

namespace ECommerce.Application.Abstractions.Files;

public sealed record CatalogCsvParseResult(
    IReadOnlyCollection<CatalogCsvRow> Rows,
    IReadOnlyCollection<Error> Errors);

public interface ICatalogCsvParser
{
    Task<CatalogCsvParseResult> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken = default);
}
