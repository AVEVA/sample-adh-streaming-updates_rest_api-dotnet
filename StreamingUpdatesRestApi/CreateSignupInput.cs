namespace StreamingUpdatesRestApi
{
    public class CreateSignupInput
    {
        // <summary>
        /// Signup Name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Resource type of the resource identifiers.
        /// </summary>
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// Collection of resource identifiers.
        /// </summary>
        public IEnumerable<string> ResourceIds { get; set; }
    }
}
