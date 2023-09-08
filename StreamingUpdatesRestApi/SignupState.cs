using System.Text.Json.Serialization;

namespace StreamingUpdatesRestApi
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SignupState
    {
        Activating = 0,

        Active = 1,

        Expired = 2,
    }
}