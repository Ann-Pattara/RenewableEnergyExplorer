using System.Net.Http.Json;
using RenewableEnergyBlazor.Models;
using RenewableEnergyContracts;

namespace RenewableEnergyBlazor.Services;

/// <summary>
/// Fetches renewable energy documents from the API.
/// All paths are relative — the HttpClient base address is set to the app
/// origin in Program.cs, so this works identically in dev and production.
/// </summary>
public class EnergyApiService(HttpClient http, ILogger<EnergyApiService> logger)
{
    // Relative path — resolves against HttpClient.BaseAddress (the app origin)
    private const string SearchPath = "api/energy/search";

    public async Task<SearchResult> SearchAsync(SearchParameters parameters)
    {
        try
        {
            var url = $"{SearchPath}?{BuildQueryString(parameters)}";
            logger.LogInformation("Querying: {Url}", url);

            var response = await http.GetFromJsonAsync<ApiResponse<List<EnergyDocument>>>(url);

            if (response is null || !response.Success)
                return SearchResult.Empty(response?.Message ?? "No results returned.");

            return new SearchResult
            {
                Documents    = response.Data ?? [],
                Pagination   = response.Pagination,
                ErrorMessage = null
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "API request failed");
            return SearchResult.Empty("Could not reach the energy data API.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected search error");
            return SearchResult.Empty("An unexpected error occurred.");
        }
    }

    private static string BuildQueryString(SearchParameters p)
    {
        var parts = new List<string>
        {
            $"topic={Uri.EscapeDataString(p.Topic)}",
            $"page={p.Page}",
            $"pageSize={p.PageSize}",
            $"sortBy={Uri.EscapeDataString(p.SortBy)}",
            $"sortOrder={Uri.EscapeDataString(p.SortOrder)}"
        };

        if (!string.IsNullOrWhiteSpace(p.Keyword))
            parts.Add($"keyword={Uri.EscapeDataString(p.Keyword)}");

        if (!string.IsNullOrWhiteSpace(p.Country))
            parts.Add($"country={Uri.EscapeDataString(p.Country)}");

        if (p.StartDate.HasValue)
            parts.Add($"startDate={p.StartDate.Value:yyyy-MM-dd}");

        if (p.EndDate.HasValue)
            parts.Add($"endDate={p.EndDate.Value:yyyy-MM-dd}");

        return string.Join("&", parts);
    }
}
