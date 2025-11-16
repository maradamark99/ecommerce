using System.Text.Json;

namespace EcommerceLib;

public static class JsonHelper
{
    public static JsonElement ToJsonElement(object value)
    {
        var json = JsonSerializer.Serialize(value);
        
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}