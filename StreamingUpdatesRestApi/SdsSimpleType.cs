using OSIsoft.Data;

namespace StreamingUpdatesRestApi
{
    public class SdsSimpleType
    {
        [SdsMember(IsKey = true)]
        public DateTime Timestamp { get; set; }

        public double Value { get; set; }
    }
}
