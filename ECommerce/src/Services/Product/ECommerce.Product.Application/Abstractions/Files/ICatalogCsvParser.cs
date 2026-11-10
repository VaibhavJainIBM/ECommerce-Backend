using System;
using System.Collections.Generic;
using System.Text;

using ECommerce.Product.Application.Common;
using ECommerce.Product.Application.Catalog.Importing;

namespace ECommerce.Product.Application.Abstractions.Files;

public sealed record CatalogCsvParseResult(
    IReadOnlyCollection<CatalogCsvRow> Rows,
    IReadOnlyCollection<Error> Errors);

public interface ICatalogCsvParser
{
    Task<CatalogCsvParseResult> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken = default);
}
