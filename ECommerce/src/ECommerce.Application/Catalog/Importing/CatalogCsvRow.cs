using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Catalog.Importing;

public sealed record CatalogCsvRow(
    int RowNumber,
    string? ProductKey,
    string? Title,
    string? BrandName,
    string? Description,
    string? VariantName,
    string? VariantCode,
    string? Gtin);
