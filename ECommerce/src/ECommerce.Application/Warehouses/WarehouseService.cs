using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Common;
using ECommerce.Application.Warehouses.Dtos;
using ECommerce.Application.Warehouses.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Application.Warehouses;

public sealed class WarehouseService(
    IWarehouseRepository repository)
    : IWarehouseService
{
    public async Task<Result<WarehouseResponseDto>> CreateAsync(
        Guid sellerId,
        CreateWarehouseRequestDto? request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateCreate(sellerId, request);

        if (errors.Count > 0)
        {
            return Result<WarehouseResponseDto>.Failure(errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sellerStatus =
            await repository.GetSellerStatusAsync(
                sellerId,
                cancellationToken);

        if (!sellerStatus.HasValue)
        {
            return Result<WarehouseResponseDto>.Failure(
                WarehouseErrors.SellerNotFound);
        }

        if (sellerStatus.Value != SellerStatus.Active)
        {
            return Result<WarehouseResponseDto>.Failure(
                WarehouseErrors.SellerUnavailable(
                    sellerStatus.Value.ToString()));
        }

        var addressRequest = request!.Address!;

        var address = new Address(
            line1: addressRequest.Line1!.Trim(),
            city: addressRequest.City!.Trim(),
            stateOrProvince:
                addressRequest.StateOrProvince!.Trim(),
            postalCode: addressRequest.PostalCode!.Trim(),
            countryCode: addressRequest.CountryCode!.Trim(),
            line2: string.IsNullOrWhiteSpace(
                addressRequest.Line2)
                ? null
                : addressRequest.Line2.Trim());

        var warehouse = new Warehouse(
            sellerId,
            request.Name!.Trim(),
            request.Code!.Trim(),
            address);

        var outcome = await repository.TryCreateAsync(
            warehouse,
            cancellationToken);

        if (outcome == WarehouseCreateOutcome.DuplicateCode)
        {
            return Result<WarehouseResponseDto>.Failure(
                WarehouseErrors.DuplicateCode);
        }

        return Result<WarehouseResponseDto>.Success(
            Map(warehouse));
    }

    public async Task<
        Result<IReadOnlyCollection<WarehouseResponseDto>>>
        GetForSellerAsync(
            Guid sellerId,
            CancellationToken cancellationToken = default)
    {
        if (sellerId == Guid.Empty)
        {
            return Result<
                IReadOnlyCollection<WarehouseResponseDto>>
                .Failure(WarehouseErrors.SellerIdRequired);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sellerStatus =
            await repository.GetSellerStatusAsync(
                sellerId,
                cancellationToken);

        if (!sellerStatus.HasValue)
        {
            return Result<
                IReadOnlyCollection<WarehouseResponseDto>>
                .Failure(WarehouseErrors.SellerNotFound);
        }

        var warehouses =
            await repository.GetForSellerAsync(
                sellerId,
                cancellationToken);

        var response = warehouses
            .Select(Map)
            .ToArray();

        return Result<
            IReadOnlyCollection<WarehouseResponseDto>>
            .Success(response);
    }

    public async Task<Result<WarehouseResponseDto>> GetByIdAsync(
        Guid sellerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateIds(sellerId, warehouseId);

        if (errors.Count > 0)
        {
            return Result<WarehouseResponseDto>.Failure(errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var warehouse = await repository.FindByIdAsync(
            sellerId,
            warehouseId,
            cancellationToken);

        if (warehouse is null)
        {
            return Result<WarehouseResponseDto>.Failure(
                WarehouseErrors.WarehouseNotFound(warehouseId));
        }

        return Result<WarehouseResponseDto>.Success(
            Map(warehouse));
    }

    public async Task<Result<WarehouseResponseDto>> ActivateAsync(
        Guid sellerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateIds(sellerId, warehouseId);

        if (errors.Count > 0)
        {
            return Result<WarehouseResponseDto>.Failure(errors);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sellerStatus =
            await repository.GetSellerStatusAsync(
                sellerId,
                cancellationToken);

        if (!sellerStatus.HasValue)
        {
            return Result<WarehouseResponseDto>.Failure(
                WarehouseErrors.SellerNotFound);
        }

        if (sellerStatus.Value != SellerStatus.Active)
        {
            return Result<WarehouseResponseDto>.Failure(
                WarehouseErrors.SellerUnavailable(
                    sellerStatus.Value.ToString()));
        }

        var warehouse = await repository.GetTrackedAsync(
            sellerId,
            warehouseId,
            cancellationToken);

        if (warehouse is null)
        {
            return Result<WarehouseResponseDto>.Failure(
                WarehouseErrors.WarehouseNotFound(warehouseId));
        }

        warehouse.Activate();

        await repository.SaveChangesAsync(
            cancellationToken);

        return Result<WarehouseResponseDto>.Success(
            Map(warehouse));
    }

    private static List<Error> ValidateCreate(
        Guid sellerId,
        CreateWarehouseRequestDto? request)
    {
        var errors = new List<Error>();

        if (sellerId == Guid.Empty)
        {
            errors.Add(WarehouseErrors.SellerIdRequired);
        }

        if (request is null)
        {
            errors.Add(WarehouseErrors.RequestRequired);
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(WarehouseErrors.NameRequired);
        }
        else if (request.Name.Trim().Length > 150)
        {
            errors.Add(WarehouseErrors.NameTooLong);
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            errors.Add(WarehouseErrors.CodeRequired);
        }
        else
        {
            var code = request.Code.Trim();

            if (code.Length > 50)
            {
                errors.Add(WarehouseErrors.CodeTooLong);
            }
            else if (code.Any(character =>
                         !IsAllowedCodeCharacter(character)))
            {
                errors.Add(WarehouseErrors.CodeInvalid);
            }
        }

        ValidateAddress(request.Address, errors);

        return errors;
    }

    private static void ValidateAddress(
        CreateWarehouseAddressRequestDto? address,
        ICollection<Error> errors)
    {
        if (address is null)
        {
            errors.Add(WarehouseErrors.AddressRequired);
            return;
        }

        if (string.IsNullOrWhiteSpace(address.Line1))
        {
            errors.Add(WarehouseErrors.AddressLine1Required);
        }
        else if (address.Line1.Trim().Length > 200)
        {
            errors.Add(WarehouseErrors.AddressLine1TooLong);
        }

        if (!string.IsNullOrWhiteSpace(address.Line2) &&
            address.Line2.Trim().Length > 200)
        {
            errors.Add(WarehouseErrors.AddressLine2TooLong);
        }

        if (string.IsNullOrWhiteSpace(address.City))
        {
            errors.Add(WarehouseErrors.CityRequired);
        }
        else if (address.City.Trim().Length > 100)
        {
            errors.Add(WarehouseErrors.CityTooLong);
        }

        if (string.IsNullOrWhiteSpace(
                address.StateOrProvince))
        {
            errors.Add(WarehouseErrors.StateRequired);
        }
        else if (
            address.StateOrProvince.Trim().Length > 100)
        {
            errors.Add(WarehouseErrors.StateTooLong);
        }

        if (string.IsNullOrWhiteSpace(address.PostalCode))
        {
            errors.Add(WarehouseErrors.PostalCodeRequired);
        }
        else if (address.PostalCode.Trim().Length > 20)
        {
            errors.Add(WarehouseErrors.PostalCodeTooLong);
        }

        if (string.IsNullOrWhiteSpace(address.CountryCode))
        {
            errors.Add(WarehouseErrors.CountryCodeRequired);
        }
        else
        {
            var countryCode =
                address.CountryCode.Trim();

            if (countryCode.Length != 2 ||
                countryCode.Any(character =>
                    !IsAsciiLetter(character)))
            {
                errors.Add(WarehouseErrors.CountryCodeInvalid);
            }
        }
    }

    private static List<Error> ValidateIds(
        Guid sellerId,
        Guid warehouseId)
    {
        var errors = new List<Error>();

        if (sellerId == Guid.Empty)
        {
            errors.Add(WarehouseErrors.SellerIdRequired);
        }

        if (warehouseId == Guid.Empty)
        {
            errors.Add(WarehouseErrors.WarehouseIdRequired);
        }

        return errors;
    }

    private static bool IsAllowedCodeCharacter(char character)
    {
        return IsAsciiLetter(character) ||
               character is >= '0' and <= '9' ||
               character is '-' or '_';
    }

    private static bool IsAsciiLetter(char character)
    {
        return character is >= 'A' and <= 'Z' ||
               character is >= 'a' and <= 'z';
    }

    private static WarehouseResponseDto Map(
        Warehouse warehouse)
    {
        var address = warehouse.Address;

        return new WarehouseResponseDto(
            warehouse.Id,
            warehouse.SellerId,
            warehouse.Name,
            warehouse.Code,
            warehouse.Status.ToString(),
            new WarehouseAddressResponseDto(
                address.Line1,
                address.Line2,
                address.City,
                address.StateOrProvince,
                address.PostalCode,
                address.CountryCode),
            warehouse.CreatedAtUtc,
            warehouse.UpdatedAtUtc);
    }
}