namespace ChangeBrokerRestApi
{
    [Serializable]
    public class SignupWithBookmark : Signup
    {
        public string Bookmark { get; set; }
    }
}
