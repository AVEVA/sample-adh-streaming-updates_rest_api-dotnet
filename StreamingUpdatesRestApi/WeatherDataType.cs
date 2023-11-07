using OSIsoft.Data;

namespace StreamingUpdatesRestApi
{
    public class WeatherDataType
    {
        [SdsMember(IsKey = true)]
        public DateTime Timestamp { get; set; }

        public double Humidity { get; set; }
        public double Temperature { get; set; }
    }
}
