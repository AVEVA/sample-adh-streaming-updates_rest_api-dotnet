namespace ChangeBrokerRestApi
{
    public class SignupResourcesInput
    {
        public IEnumerable<string>? ResourcesToAdd { get; set; }

        public IEnumerable<string>? ResourcesToRemove { get; set; }
    }
}
