namespace StreamingUpdatesRestApi
{
    public class Update
    {
        public string ResourceId { get; set; }
        public string Operation { get; set; }
        public IEnumerable<SdsSimpleType> Events { get; set; }
    }
}
