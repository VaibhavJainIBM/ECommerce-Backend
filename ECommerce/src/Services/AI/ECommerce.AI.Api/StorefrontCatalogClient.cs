using System.Net.Http.Json;

namespace ECommerce.AI.Api;

public sealed class StorefrontCatalogClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<StorefrontListing>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var result = new List<StorefrontListing>();
        var page = 1;

        while (true)
        {
            var response = await httpClient.GetFromJsonAsync<StorefrontPage>(
                $"api/storefront/listings?page={page}&pageSize=50",
                cancellationToken);

            if (response is null)
            {
                throw new InvalidOperationException(
                    "Storefront returned an empty response.");
            }

            result.AddRange(response.Items);

            if (response.TotalPages == 0 || page >= response.TotalPages)
            {
                break;
            }

            page++;
        }

        return result;
    }
}
