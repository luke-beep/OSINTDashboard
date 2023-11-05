using OSINTDashboard.Models;
using System.Text.Json;

namespace OSINTDashboard.Utilities;

public class JsonConverter
{
    public static SearchResultViewModel DeserializeJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<SearchResultViewModel>(json, options) ?? throw new InvalidOperationException() ;
    }
}