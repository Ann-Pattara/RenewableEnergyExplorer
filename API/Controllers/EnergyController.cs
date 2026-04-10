using Microsoft.AspNetCore.Mvc;
using RenewableEnergyAPI.Models;
using RenewableEnergyAPI.Services;
using RenewableEnergyContracts;

namespace RenewableEnergyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EnergyController(IWorldBankService worldBank, ILogger<EnergyController> logger)
    : ControllerBase
{
    private static readonly HashSet<string> ValidTopics =
        new(StringComparer.OrdinalIgnoreCase) { "wind", "solar", "all" };

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<EnergyDocument>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> Search([FromQuery] EnergySearchQuery query)
    {
        if (!ValidTopics.Contains(query.Topic))
            return BadRequest(ApiResponse<object>.Fail(
                $"Invalid topic '{query.Topic}'. Must be one of: wind, solar, all."));

        if (!string.IsNullOrEmpty(query.StartDate) && !IsValidDate(query.StartDate))
            return BadRequest(ApiResponse<object>.Fail("StartDate must be in YYYY-MM-DD format."));

        if (!string.IsNullOrEmpty(query.EndDate) && !IsValidDate(query.EndDate))
            return BadRequest(ApiResponse<object>.Fail("EndDate must be in YYYY-MM-DD format."));

        try
        {
            var (documents, total) = await worldBank.SearchAsync(query);

            var totalPages = (int)Math.Ceiling((double)total / query.PageSize);
            var pagination = new PaginationMeta(query.Page, query.PageSize, total, totalPages);

            logger.LogInformation("Returned {Count}/{Total} docs for topic={Topic}",
                documents.Count, total, query.Topic);

            return Ok(ApiResponse<List<EnergyDocument>>.Ok(documents, pagination));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "World Bank API call failed");
            return StatusCode(StatusCodes.Status502BadGateway, ApiResponse<object>.Fail(
                "Could not reach the World Bank data source. Please try again shortly."));
        }
        catch (TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, ApiResponse<object>.Fail(
                "Request to World Bank API timed out."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in Energy search");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail(
                "An unexpected error occurred. Please contact support."));
        }
    }

    [HttpGet("topics")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetTopics() => Ok(ApiResponse<object>.Ok(new[]
    {
        new { Value = "all", Label = "All Renewable Energy" },
        new { Value = "wind", Label = "Wind Energy" },
        new { Value = "solar", Label = "Solar Energy" }
    }));

    [HttpGet("countries")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountries()
    {
        try
        {
            var countries = await worldBank.GetCountriesAsync();
            return Ok(ApiResponse<List<string>>.Ok(countries));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch country list");
            return Ok(ApiResponse<List<string>>.Ok(new List<string>()));
        }
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new
    {
        Status = "Good",
        Timestamp = DateTime.UtcNow,
        Service = "Renewable Energy Explorer API"
    });

    private static bool IsValidDate(string date) =>
        DateTime.TryParseExact(date, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _);
}
