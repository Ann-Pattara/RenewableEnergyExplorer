using System.ComponentModel.DataAnnotations;

namespace RenewableEnergyAPI.Models;

public class EnergySearchQuery : IValidatableObject
{
    public string Topic { get; set; } = "all";
    public string? Keyword { get; set; }
    public string? Country { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be >= 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "PageSize must be between 1 and 50.")]
    public int PageSize { get; set; } = 10;

    public string SortBy { get; set; } = "DatePublished";
    public string SortOrder { get; set; } = "desc";

    private static readonly HashSet<string> ValidSortByValues =
        new(StringComparer.OrdinalIgnoreCase) { "DatePublished", "Title", "Country" };

    private static readonly HashSet<string> ValidSortOrderValues =
        new(StringComparer.OrdinalIgnoreCase) { "asc", "desc" };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ValidSortByValues.Contains(SortBy))
            yield return new ValidationResult(
                "SortBy must be one of: DatePublished, Title, Country.",
                new[] { nameof(SortBy) });

        if (!ValidSortOrderValues.Contains(SortOrder))
            yield return new ValidationResult(
                "SortOrder must be one of: asc, desc.",
                new[] { nameof(SortOrder) });

        if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(EndDate)
            && DateTime.TryParse(StartDate, out var start) && DateTime.TryParse(EndDate, out var end)
            && start > end)
        {
            yield return new ValidationResult(
                "StartDate must be before EndDate.",
                new[] { nameof(StartDate), nameof(EndDate) });
        }
    }
}
