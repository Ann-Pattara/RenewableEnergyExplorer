using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using RenewableEnergyAPI.Models;
using RenewableEnergyContracts;

namespace RenewableEnergyAPI.Services;

public interface IWorldBankService
{
    Task<(List<EnergyDocument> Documents, int Total)> SearchAsync(EnergySearchQuery query);
}

public class WorldBankService(
    HttpClient http,
    ILogger<WorldBankService> logger,
    IMemoryCache cache) : IWorldBankService
{
    private static readonly Dictionary<string, string> TopicTerms = new()
    {
        ["wind"]  = "wind energy",
        ["solar"] = "solar energy",
        ["all"]   = "wind energy solar"
    };

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<(List<EnergyDocument> Documents, int Total)> SearchAsync(EnergySearchQuery query)
    {
        var cacheKey = BuildCacheKey(query);
        if (cache.TryGetValue(cacheKey, out (List<EnergyDocument>, int) cached))
        {
            logger.LogInformation("Cache hit: {Key}", cacheKey);
            return cached;
        }

        var url = BuildUrl(query);
        logger.LogInformation("World Bank request: {Url}", url);

        var sw = Stopwatch.StartNew();
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        sw.Stop();

        logger.LogInformation("World Bank API responded in {ElapsedMs}ms", sw.ElapsedMilliseconds);

        var json   = await response.Content.ReadAsStringAsync();
        var result = ParseResponse(json, query.Topic.ToLower());

        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    private static string BuildUrl(EnergySearchQuery query)
    {
        var topic      = query.Topic.ToLower();
        var titleQuery = TopicTerms[topic];
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            titleQuery += $" {query.Keyword}";

        var qs = new Dictionary<string, string>
        {
            ["format"]        = "json",
            ["display_title"] = titleQuery,
            ["fl"]            = "id,display_title,docty,docdt,url,abstracts,count",
            ["rows"]          = Math.Clamp(query.PageSize, 1, 50).ToString(),
            ["os"]            = ((query.Page - 1) * query.PageSize).ToString(),
            ["srt"]           = MapSortField(query.SortBy),
            ["order"]         = query.SortOrder.ToLower() == "asc" ? "asc" : "desc"
        };

        if (!string.IsNullOrWhiteSpace(query.Country))
            qs["count_exact"] = query.Country;

        if (!string.IsNullOrWhiteSpace(query.StartDate))
            qs["strdate"] = query.StartDate;

        if (!string.IsNullOrWhiteSpace(query.EndDate))
            qs["enddate"] = query.EndDate;

        return "wds?" + string.Join("&", qs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
    }

    private static (List<EnergyDocument>, int) ParseResponse(string json, string topic)
    {
        using var doc  = JsonDocument.Parse(json);
        var root       = doc.RootElement;
        var documents  = new List<EnergyDocument>();

        var total = 0;
        if (root.TryGetProperty("total", out var totalEl))
        {
            if (totalEl.ValueKind == JsonValueKind.Number)
                total = totalEl.GetInt32();
            else
                int.TryParse(totalEl.GetString(), out total);
        }

        if (!root.TryGetProperty("documents", out var docs))
            return (documents, total);

        foreach (var prop in docs.EnumerateObject())
        {
            if (prop.Name == "facets") continue;

            var d = prop.Value;
            documents.Add(new EnergyDocument
            {
                Id            = GetString(d, "id"),
                Title         = GetString(d, "display_title"),
                DocumentType  = GetString(d, "docty"),
                Country       = GetString(d, "count"),
                DatePublished = GetString(d, "docdt"),
                Url           = GetString(d, "url"),
                Abstract      = GetString(d, "abstracts"),
                Topic         = InferTopic(GetString(d, "display_title"), topic)
            });
        }

        return (documents, total);
    }

    private static string GetString(JsonElement el, string key) =>
        el.TryGetProperty(key, out var val)
            ? val.ValueKind == JsonValueKind.String ? val.GetString() ?? "" : val.ToString()
            : "";

    private static string InferTopic(string title, string queryTopic)
    {
        if (queryTopic != "all") return queryTopic;
        var t = title.ToLower();
        return (t.Contains("wind"), t.Contains("solar")) switch
        {
            (true,  true)  => "Wind & Solar",
            (true,  false) => "Wind",
            (false, true)  => "Solar",
            _              => "Renewable Energy"
        };
    }

    private static string MapSortField(string sortBy) => sortBy.ToLower() switch
    {
        "datepublished" => "docdt",
        "title"         => "display_title",
        "country"       => "count",
        _               => "docdt"
    };

    private static string BuildCacheKey(EnergySearchQuery q) =>
        $"{q.Topic}|{q.Keyword}|{q.Country}|{q.StartDate}|{q.EndDate}|{q.Page}|{q.PageSize}|{q.SortBy}|{q.SortOrder}";
}
