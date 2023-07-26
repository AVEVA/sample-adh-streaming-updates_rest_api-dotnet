using System.Text.Json.Serialization;

namespace StreamingUpdatesRestApi
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResourceType
    {
        /// <summary>
        /// Stream resource.
        /// </summary>
        Stream = 0,
    }
}