using System.Text.Json.Serialization;

namespace StreamingUpdatesRestApi
{
    /// <summary>
    /// Filter to be applied to Signup Resources.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SignupResourceFilter
    {
        /// <summary>
        /// Returns inaccessible signup resources.
        /// </summary>
        Inaccessible = 0,

        /// <summary>
        /// Returns accessible signup resources.
        /// </summary>
        Accessible = 1,

        /// <summary>
        /// Returns accessible and inaccessible signup resources.
        /// </summary>
        All = 2,
    }
}
