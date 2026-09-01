using ECommerce.Application.Authorization;
using ECommerce.Application.Catalog;
using ECommerce.Application.Catalog.Dtos;
using ECommerce.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Authorize(
    Roles = PlatformRoleNames.PlatformAdmin)]
[ApiController]
[Route("api/admin/catalog/products")]
public sealed class AdminCatalogController(
    IAdminCatalogService catalogService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(
        typeof(CreateProductResponseDto),
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
    public async Task<ActionResult<CreateProductResponseDto>>
        CreateProductAsync(
            [FromBody] CreateProductRequestDto? request,
            CancellationToken cancellationToken)
    {
        var result = await catalogService.CreateProductAsync(
            request,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return StatusCode(
            StatusCodes.Status201Created,
            result.Value!);
    }

    [HttpPost("{productId:guid}/activate")]
    [ProducesResponseType(
    typeof(CreateProductResponseDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateProductResponseDto>>
    ActivateProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var result = await catalogService.ActivateProductAsync(
            productId,
            cancellationToken);

        if (result.IsFailure)
        {
            return ToProblem(result.Errors);
        }

        return Ok(result.Value!);
    }

    private ActionResult ToProblem(
    IReadOnlyCollection<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new InvalidOperationException(
                "A failed catalog result contained no errors.");
        }

        var notFound = errors.FirstOrDefault(error =>
            string.Equals(
                error.Code,
                CatalogErrors.ProductNotFoundCode,
                StringComparison.Ordinal));

        if (notFound is not null)
        {
            return ApiProblem(
                StatusCodes.Status404NotFound,
                "Catalog product not found.",
                notFound.Description,
                notFound.Code);
        }

        var conflict = errors.FirstOrDefault(error =>
            string.Equals(
                error.Code,
                CatalogErrors.GtinConflictCode,
                StringComparison.Ordinal) ||
            string.Equals(
                error.Code,
                CatalogErrors.ActivationConflictCode,
                StringComparison.Ordinal));

        if (conflict is not null)
        {
            return ApiProblem(
                StatusCodes.Status409Conflict,
                "Catalog conflict.",
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
                    "One or more validation errors occurred.",
                Detail =
                    "Correct the supplied values and try again.",
                Instance = HttpContext.Request.Path
            };

        problemDetails.Extensions["traceId"] =
            HttpContext.TraceIdentifier;

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
                ["code"] = code,
                ["traceId"] =
                    HttpContext.TraceIdentifier
            });
    }
}