using System.Text.Json.Serialization;

namespace StreamingUpdatesRestApi
{
    /// <summary>
    /// Signup Status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SignupState
    {
        /// <summary>
        /// Signup is being activated.
        /// </summary>
        Activating = 0,

        /// <summary>
        /// Signup is active.
        /// </summary>
        Active = 1,

        /// <summary>
        /// Signup is expired.
        /// </summary>
        Expired = 2,
    }
}