using System.Text.Json.Serialization;

namespace StreamingUpdatesRestApi
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResourceType
    {
        Stream = 0,
    }
}