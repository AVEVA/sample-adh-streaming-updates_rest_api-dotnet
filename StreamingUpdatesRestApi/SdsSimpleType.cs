using OSIsoft.Data;

namespace StreamingUpdatesRestApi
{
    public class SdsSimpleType
    {
        [SdsMember(IsKey = true)]
        public DateTime Time { get; set; }

        public double Value { get; set; }
    }
}
