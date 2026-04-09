using RenewableEnergyContracts;

namespace RenewableEnergyBlazor.Models;

public class SearchParameters
{
    public string Topic { get; set; } = "all";
    public string Keyword { get; set; } = "";
    public string Country { get; set; } = "";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "DatePublished";
    public string SortOrder { get; set; } = "desc";
}

public class SearchResult
{
    public List<EnergyDocument> Documents { get; set; } = [];
    public PaginationMeta? Pagination { get; set; }
    public string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public static SearchResult Empty(string? error = null) => new()
    {
        Documents    = [],
        Pagination   = null,
        ErrorMessage = error
    };
}
