namespace ChangeBrokerRestApi
{
    public class CreateSignupInput
    {
        public string? Name { get; set; }

        public ResourceType ResourceType { get; set; }

        public IEnumerable<string> ResourceIds { get; set; }
    }
}
