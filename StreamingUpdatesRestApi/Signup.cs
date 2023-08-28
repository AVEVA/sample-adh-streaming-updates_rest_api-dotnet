namespace StreamingUpdatesRestApi
{
    [Serializable]
    public class Signup
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public ResourceType Type { get; set; }

        public DateTimeOffset CreatedDate { get; set; }

        public DateTimeOffset ModifiedDate { get; set; }

        public SignupState SignupState { get; set; }
    }
}
