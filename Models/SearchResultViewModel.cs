using System.Text.Json.Serialization;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace OSINTDashboard.Models;

public class SearchResultViewModel
{
    [JsonPropertyName("List")]
    public Dictionary<string, SearchResultItem> List { get; set; }

    [JsonPropertyName("NumOfDatabase")]
    public int NumOfDatabase { get; set; }

    [JsonPropertyName("search time")]
    public double SearchTime { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("free_requests_left")]
    public int FreeRequestsLeft { get; set; }
}

public class SearchResultItem
{
    [JsonPropertyName("Data")]
    public List<Dictionary<string, object>> Data { get; set; }

    [JsonPropertyName("NumOfResults")]
    public int NumOfResults { get; set; }

    [JsonPropertyName("InfoLeak")]
    public string InfoLeak { get; set; }
}