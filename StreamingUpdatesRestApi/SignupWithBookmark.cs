namespace StreamingUpdatesRestApi
{
    [Serializable]
    public class SignupWithBookmark
    {
        public Signup Signup { get; set; }

        public string Bookmark { get; set; }
    }
}
