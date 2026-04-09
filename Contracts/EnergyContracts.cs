namespace RenewableEnergyContracts;

public class ApiResponse<T>
{
    public bool   Success    { get; set; }
    public string? Message   { get; set; }
    public T?      Data      { get; set; }
    public PaginationMeta? Pagination { get; set; }

    public static ApiResponse<T> Ok(T data, PaginationMeta? pagination = null) =>
        new() { Success = true, Data = data, Pagination = pagination };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

public record PaginationMeta(int Page, int PageSize, int TotalResults, int TotalPages);

public class EnergyDocument
{
    public string  Id           { get; set; } = string.Empty;
    public string  Title        { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? Country      { get; set; }
    public string? DatePublished { get; set; }
    public string? Url          { get; set; }
    public string? Topic        { get; set; }
    public string? Abstract     { get; set; }
}
