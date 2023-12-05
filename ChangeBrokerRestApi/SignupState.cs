using System.Text.Json.Serialization;

namespace ChangeBrokerRestApi
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SignupState
    {
        Activating = 0,

        Active = 1,

        Expired = 2,
    }
}