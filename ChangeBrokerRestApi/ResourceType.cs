using System.Text.Json.Serialization;

namespace ChangeBrokerRestApi
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResourceType
    {
        Stream = 0,
    }
}
