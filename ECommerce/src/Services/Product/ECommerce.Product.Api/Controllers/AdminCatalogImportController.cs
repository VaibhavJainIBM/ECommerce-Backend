using ECommerce.Product.Application.Authorization;
using ECommerce.Product.Application.Catalog.Importing;
using ECommerce.Product.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Product.Api.Controllers;

[Authorize(
    Roles = PlatformRoleNames.PlatformAdmin)]
[ApiController]
[Route("api/admin/catalog/products")]
public sealed class AdminCatalogImportController(
    IAdminCatalogImportService importService)
    : ControllerBase
{
    private const long MaximumFileSize =
        2 * 1024 * 1024;

    [HttpPost("import-csv")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2_200_000)]
    public async Task<ActionResult<CatalogImportResponseDto>>
        ImportCsvAsync(
            [FromForm] IFormFile? file,
            [FromForm] bool activate,
            CancellationToken cancellationToken)
    {
        var fileError =
            ValidateFile(file);

        if (fileError is not null)
        {
            return ToProblem([fileError]);
        }

        await using var stream =
            file!.OpenReadStream();

        var result =
            await importService.ImportAsync(
                stream,
                activate,
                cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(
                result.Errors);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Value!);
    }

    private static Error? ValidateFile(
        IFormFile? file)
    {
        if (file is null)
        {
            return CatalogImportErrors.FileRequired;
        }

        if (file.Length == 0)
        {
            return CatalogImportErrors.FileEmpty;
        }

        if (file.Length > MaximumFileSize)
        {
            return CatalogImportErrors.FileTooLarge;
        }

        if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".csv",
                StringComparison.OrdinalIgnoreCase))
        {
            return CatalogImportErrors.InvalidFileExtension;
        }

        return null;
    }

    private ActionResult ToProblem(
        IReadOnlyCollection<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new InvalidOperationException(
                "A failed catalog import contained no errors.");
        }

        var first = errors.First();

        if (first.Code ==
            CatalogImportErrors.FileTooLargeCode)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status413PayloadTooLarge,
                title:
                    "Catalog import is too large.",
                detail:
                    first.Description);
        }

        var conflict =
            errors.FirstOrDefault(error =>
                error.Code ==
                    CatalogImportErrors
                        .GtinConflictCode);

        if (conflict is not null)
        {
            return Problem(
                statusCode:
                    StatusCodes.Status409Conflict,
                title:
                    "Catalog import conflict.",
                detail:
                    conflict.Description);
        }

        var grouped =
            errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error =>
                            error.Description)
                        .Distinct()
                        .ToArray());

        return ValidationProblem(
            new ValidationProblemDetails(grouped)
            {
                Status =
                    StatusCodes.Status400BadRequest,
                Title =
                    "One or more CSV validation errors occurred.",
                Detail =
                    "Correct the CSV file and try the import again."
            });
    }
}