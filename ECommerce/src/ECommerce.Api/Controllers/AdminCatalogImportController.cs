using ECommerce.Application.Authorization;
using ECommerce.Application.Catalog.Importing;
using ECommerce.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize(
    Roles = PlatformRoleNames.PlatformAdmin)]
[ApiController]
[Route("api/admin/catalog/products")]
public sealed class AdminCatalogImportController(
    IAdminCatalogImportService importService)
    : ControllerBase
{
    private const long MaximumFileSize = 2 * 1024 * 1024;

    [HttpPost("import-csv")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2_200_000)]
    [ProducesResponseType(
        typeof(CatalogImportResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ValidationProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<CatalogImportResponseDto>>
        ImportCsvAsync(
            [FromForm] IFormFile? file,
            [FromForm] bool activate,
            CancellationToken cancellationToken)
    {
        var fileError = ValidateFile(file);

        if (fileError is not null)
        {
            return ToProblem([fileError]);
        }

        await using var stream = file!.OpenReadStream();

        var result = await importService.ImportAsync(
            stream,
            activate,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Value!);
    }

    private static Error? ValidateFile(IFormFile? file)
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

        if (string.Equals(
                first.Code,
                CatalogImportErrors.FileTooLargeCode,
                StringComparison.Ordinal))
        {
            return ApiProblem(
                StatusCodes.Status413PayloadTooLarge,
                "Catalog import is too large.",
                first.Description,
                first.Code);
        }

        var conflict = errors.FirstOrDefault(error =>
            string.Equals(
                error.Code,
                CatalogImportErrors.GtinConflictCode,
                StringComparison.Ordinal));

        if (conflict is not null)
        {
            return ApiProblem(
                StatusCodes.Status409Conflict,
                "Catalog import conflict.",
                conflict.Description,
                conflict.Code);
        }

        return ValidationProblemResponse(errors);
    }

    private ActionResult ValidationProblemResponse(
        IReadOnlyCollection<Error> errors)
    {
        var groupedErrors = errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.Description)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        var problemDetails =
            new ValidationProblemDetails(groupedErrors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title =
                    "One or more CSV validation errors occurred.",
                Detail =
                    "Correct the CSV file and try the import again.",
                Instance = HttpContext.Request.Path
            };

        return ValidationProblem(problemDetails);
    }

    private ObjectResult ApiProblem(
        int statusCode,
        string title,
        string detail,
        string code)
    {
        return Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            instance: HttpContext.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code
            });
    }
}
